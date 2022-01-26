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
        private readonly Dictionary<string, Func<Task>> executionHandlers = new();

        public PerperStartup(string agent) => Agent = agent;

        public PerperStartup AddInitHandler(Func<Task> handler)
        {
            initHandlers.Add(handler);
            return this;
        }

        public PerperStartup AddHandler(string @delegate, Func<Task> handler)
        {
            executionHandlers.Add(@delegate, handler);
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
            await EnterServicesContext(() => RunInServiceContext(cancellationToken)).ConfigureAwait(false);
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

        public static async Task EnterServicesContext(Func<Task> context)
        {
            var fabricService = await EstablishConnection().ConfigureAwait(false);

            AsyncLocals.SetConnection(fabricService);

            await context().ConfigureAwait(false);

            await fabricService.DisposeAsync().ConfigureAwait(false);
        }

        public static async Task<FabricService> EstablishConnection()
        {
            var apacheIgniteEndpoint = Environment.GetEnvironmentVariable("APACHE_IGNITE_ENDPOINT") ?? "127.0.0.1:10800";
            var fabricGrpcAddress = Environment.GetEnvironmentVariable("PERPER_FABRIC_ENDPOINT") ?? "http://127.0.0.1:40400";

            Console.WriteLine($"APACHE_IGNITE_ENDPOINT: {apacheIgniteEndpoint}");
            Console.WriteLine($"PERPER_FABRIC_ENDPOINT: {fabricGrpcAddress}");

            var igniteConfiguration = new IgniteClientConfiguration
            {
                Endpoints = new List<string> { apacheIgniteEndpoint },
                BinaryConfiguration = new BinaryConfiguration
                {
                    NameMapper = PerperBinaryConfigurations.NameMapper,
                    TypeConfigurations = PerperBinaryConfigurations.TypeConfigurations,
                    ForceTimestamp = true,
                },
                SocketTimeout = TimeSpan.FromSeconds(60)
            };

            var ignite = await Policy
                .HandleInner<System.Net.Sockets.SocketException>()
                .WaitAndRetryAsync(10,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)),
                    (exception, timespan) => Console.WriteLine("Failed to connect to Ignite, retrying in {0}s", timespan.TotalSeconds))
                .ExecuteAsync(() => Task.Run(() => Ignition.StartClient(igniteConfiguration))).ConfigureAwait(false);

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var grpcChannel = GrpcChannel.ForAddress(fabricGrpcAddress);

            return new FabricService(ignite, grpcChannel);
        }

        public static string ConfigureInstance()
        {
            var instance = Environment.GetEnvironmentVariable("X_PERPER_INSTANCE") ?? "";
            Console.WriteLine($"X_PERPER_INSTANCE: {instance}");
            return instance;
        }

        #endregion Services
        #region ListenNotifications

        public Task RunInServiceContext(CancellationToken cancellationToken = default)
        {
            var instance = UseInstances ? ConfigureInstance() : null;

            var taskCollection = new TaskCollection();

            executionHandlers.TryAdd(PerperContext.StartupFunctionName, async () =>
            {
                await AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution).ConfigureAwait(false);
            });

            var initInstance = instance ?? $"{Agent}-init";
            var initExecution = new FabricExecution(Agent, initInstance, "Init", $"{initInstance}-init", cancellationToken);
            foreach (var handler in initHandlers)
            {
                taskCollection.Add(async () =>
                {
                    AsyncLocals.SetExecution(initExecution);
                    await handler().ConfigureAwait(false);
                });
            }

            foreach (var (@delegate, handler) in executionHandlers)
            {
                ListenExecutions(taskCollection, Agent, instance, @delegate, handler, cancellationToken);
            }

            return taskCollection.GetTask();
        }

        public static void ListenExecutions(TaskCollection taskCollection, string agent, string? instance, string @delegate, Func<Task> handler, CancellationToken cancellationToken)
        {
            taskCollection.Add(async () =>
            {
                await foreach (var execution in AsyncLocals.FabricService.GetExecutionsReader(agent, instance, @delegate).ReadAllAsync(cancellationToken))
                {
                    taskCollection.Add(async () =>
                    {
                        AsyncLocals.SetExecution(execution);
                        await handler().ConfigureAwait(false);
                    });
                }
            });
        }

        #endregion ListenNotifications
    }
}