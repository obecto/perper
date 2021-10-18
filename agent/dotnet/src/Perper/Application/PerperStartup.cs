using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;

using Grpc.Net.Client;

using Perper.Extensions;
using Perper.Protocol;

using Polly;

namespace Perper.Application
{
    public class PerperStartup
    {
        public string Agent { get; }
        public bool UseInstances { get; set; } = false;
        private readonly List<Func<Task>> initHandlers = new();
        private readonly Dictionary<string, Func<Task>> callHandlers = new();
        private readonly Dictionary<string, Func<Task>> streamHandlers = new();

        public PerperStartup(string agent) => Agent = agent;

        public PerperStartup AddInitHandler(Func<Task> handler)
        {
            initHandlers.Add(handler);
            return this;
        }

        public PerperStartup AddCallHandler(string @delegate, Func<Task> handler)
        {
            callHandlers.Add(@delegate, handler);
            return this;
        }

        public PerperStartup AddStreamHandler(string @delegate, Func<Task> handler)
        {
            streamHandlers.Add(@delegate, handler);
            return this;
        }

        public PerperStartup WithInstances()
        {
            UseInstances = true;
            return this;
        }

        #region RunAsync

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            await EnterServicesContext(Agent, () => RunInServiceContext(cancellationToken), UseInstances).ConfigureAwait(false);
        }

        public static Task RunAsync(string agent, CancellationToken cancellationToken = default)
        {
            return new PerperStartup(agent).DiscoverHandlersFromAssembly().RunAsync(cancellationToken);
        }

        public static Task RunAsync(string agent, string rootNamespace, CancellationToken cancellationToken = default)
        {
            return new PerperStartup(agent).DiscoverHandlersFromAssembly(null, rootNamespace).RunAsync(cancellationToken);
        }

        #endregion RunAsync
        #region Services

        public static async Task EnterServicesContext(string agent, Func<Task> context, bool useInstance = false)
        {
            var (cacheService, notificationService) = await EstablishConnection(agent, useInstance).ConfigureAwait(false);

            AsyncLocals.SetConnection(cacheService, notificationService);

            await context().ConfigureAwait(false);

            await notificationService.StopAsync().ConfigureAwait(false);
            notificationService.Dispose();
        }

        public static async Task<(CacheService, NotificationService)> EstablishConnection(string agent, bool useInstance = false)
        {
            var apacheIgniteEndpoint = Environment.GetEnvironmentVariable("APACHE_IGNITE_ENDPOINT") ?? "127.0.0.1:10800";
            var fabricGrpcAddress = Environment.GetEnvironmentVariable("PERPER_FABRIC_ENDPOINT") ?? "http://127.0.0.1:40400";
            string? instance = null;

            Console.WriteLine($"APACHE_IGNITE_ENDPOINT: {apacheIgniteEndpoint}");
            Console.WriteLine($"PERPER_FABRIC_ENDPOINT: {fabricGrpcAddress}");
            if (useInstance)
            {
                instance = Environment.GetEnvironmentVariable("X_PERPER_INSTANCE") ?? "";
                Console.WriteLine($"X_PERPER_INSTANCE: {instance}");
            }

            var igniteConfiguration = new IgniteClientConfiguration
            {
                Endpoints = new List<string> { apacheIgniteEndpoint },
                BinaryConfiguration = new BinaryConfiguration
                {
                    NameMapper = PerperBinaryConfigurations.NameMapper,
                    TypeConfigurations = PerperBinaryConfigurations.TypeConfigurations,
                    ForceTimestamp = true,
                }
            };

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var grpcChannel = GrpcChannel.ForAddress(fabricGrpcAddress);

            var ignite = await Policy
                .HandleInner<System.Net.Sockets.SocketException>()
                .WaitAndRetryAsync(10,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)),
                    (exception, timespan) => Console.WriteLine("Failed to connect to Ignite, retrying in {0}s", timespan.TotalSeconds))
                .ExecuteAsync(() => Task.Run(() => Ignition.StartClient(igniteConfiguration))).ConfigureAwait(false);

            var cacheService = new CacheService(ignite);
            var notificationService = new NotificationService(ignite, grpcChannel, agent, instance);

            await Policy
                .Handle<Grpc.Core.RpcException>(ex => ex.Status.DebugException is System.Net.Http.HttpRequestException)
                .WaitAndRetryAsync(10,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)),
                    (exception, timespan) => Console.WriteLine("Failed to connect to GRPC, retrying in {0}s", timespan.TotalSeconds))
                .ExecuteAsync(notificationService.StartAsync).ConfigureAwait(false);

            return (cacheService, notificationService);
        }

        #endregion Services
        #region ListenNotifications

        public Task RunInServiceContext(CancellationToken cancellationToken = default)
        {
            var taskCollection = new TaskCollection();

            callHandlers.TryAdd(PerperContext.StartupFunctionName, async () =>
            {
                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
            });

            foreach (var handler in initHandlers)
            {
                taskCollection.Add(AsyncLocals.EnterContext($"{AsyncLocals.Agent}-init", $"Init-init", handler));
            }

            foreach (var (@delegate, handler) in callHandlers)
            {
                ListenCallNotifications(taskCollection, @delegate, handler, cancellationToken);
            }

            foreach (var (@delegate, handler) in streamHandlers)
            {
                ListenStreamNotifications(taskCollection, @delegate, handler, cancellationToken);
            }

            return taskCollection.GetTask();
        }

        public static void ListenCallNotifications(TaskCollection taskCollection, string @delegate, Func<Task> handler, CancellationToken cancellationToken)
        {
            taskCollection.Add(async () =>
            {
                await foreach (var (instance, call) in AsyncLocals.NotificationService.GetCallTriggers(@delegate).ReadAllAsync(cancellationToken))
                {
                    taskCollection.Add(AsyncLocals.EnterContext(instance, call, handler));
                }
            });
        }

        public static void ListenStreamNotifications(TaskCollection taskCollection, string @delegate, Func<Task> handler, CancellationToken cancellationToken)
        {
            taskCollection.Add(async () =>
            {
                await foreach (var (instance, stream) in AsyncLocals.NotificationService.GetStreamTriggers(@delegate).ReadAllAsync(cancellationToken))
                {
                    taskCollection.Add(AsyncLocals.EnterContext(instance, stream, handler));
                }
            });
        }

        #endregion ListenNotifications
    }
}