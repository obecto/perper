using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;

using Grpc.Net.Client;

using Perper.Model;
using Perper.Protocol;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Service;

using Polly;

using Context = Perper.Model.Context;

namespace Perper.Application
{
    public static class PerperStartup
    {
        public const string RunAsyncMethodName = "RunAsync";
        public const string StreamsNamespaceName = "Streams";
        public const string CallsNamespaceName = "Calls";
        public const string InitCallName = "Init";

        #region RunAsync

        public static async Task RunAsync(string agent, CancellationToken cancellationToken)
        {
            await RunAsync(agent, DiscoverStreamAndCallTypes(), cancellationToken).ConfigureAwait(false);
        }

        public static async Task RunAsync(string agent, (List<Type> streamTypes, List<Type> callTypes) types, CancellationToken cancellationToken)
        {
            await EnterServicesContext(agent, () => ListenNotificationsForTypes(types, cancellationToken)).ConfigureAwait(false);
        }

        #endregion RunAsync
        #region DiscoverStreamAndCallTypes

        public static (List<Type> streamTypes, List<Type> callTypes) DiscoverStreamAndCallTypes()
        {
            return DiscoverStreamAndCallTypes(Assembly.GetEntryAssembly()!);
        }

        public static (List<Type> streamTypes, List<Type> callTypes) DiscoverStreamAndCallTypes(Assembly assembly)
        {
            return DiscoverStreamAndCallTypes(assembly, assembly.GetName().Name!);
        }

        public static (List<Type> streamTypes, List<Type> callTypes) DiscoverStreamAndCallTypes(Assembly assembly, string rootNamespace)
        {
            var streamsNamespace = $"{rootNamespace}.{StreamsNamespaceName}";
            var callsNamespace = $"{rootNamespace}.{CallsNamespaceName}";

            var types = assembly
                .GetTypes()
                .Where(t => t.IsClass && t.MemberType == MemberTypes.TypeInfo && !string.IsNullOrEmpty(t.Namespace) && t.GetMethod(RunAsyncMethodName) != null);

            var streamTypes = types
                 .Where(x => x.Namespace!.Contains(streamsNamespace, StringComparison.InvariantCultureIgnoreCase))
                 .ToList();

            var callTypes = types
                 .Where(x => x.Namespace!.Contains(callsNamespace, StringComparison.InvariantCultureIgnoreCase))
                 .ToList();

            return (streamTypes, callTypes);
        }

        #endregion DiscoverStreamAndCallTypes
        #region Services

        public static async Task EnterServicesContext(string agent, Func<Task> context)
        {
            var (cacheService, notificationService) = await InitializeServices(agent).ConfigureAwait(false);

            AsyncLocals.SetConnection(cacheService, notificationService);

            await context().ConfigureAwait(false);

            await notificationService.StopAsync().ConfigureAwait(false);
            notificationService.Dispose();
        }

        public static async Task<(CacheService, NotificationService)> InitializeServices(string agent)
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
            var notificationService = new NotificationService(ignite, grpcChannel, agent);

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

        private static Task ListenNotificationsForTypes((List<Type> streamTypes, List<Type> callTypes) types, CancellationToken cancellationToken)
        {
            var (streamTypes, callTypes) = types;

            var taskCollection = new TaskCollection();

            InvokeAndRemoveInitCall(taskCollection, ref callTypes);

            ListenCallNotifications(taskCollection, callTypes, cancellationToken);
            ListenStreamNotifications(taskCollection, streamTypes, cancellationToken);
            AcknowledgeStartupCalls(taskCollection, callTypes, cancellationToken);

            return taskCollection.GetTask();
        }

        private static void InvokeAndRemoveInitCall(TaskCollection taskCollection, ref List<Type> callTypes)
        {
            var initType = callTypes.FirstOrDefault(x => x.Name == InitCallName);
            if (initType == null)
            {
                return;
            }

            callTypes.Remove(initType);

            taskCollection.Add(AsyncLocals.EnterContext($"{AsyncLocals.Agent}-init", $"{initType.Name}-init", async () =>
            {
                var initCallInstance = InstanciateType(initType);
                var initArguments = Array.Empty<object>();

                var (returnType, invokeResult) = await InvokeMethodAsync(initType, initCallInstance, initArguments).ConfigureAwait(false);
            }));
        }

        private static void AcknowledgeStartupCalls(TaskCollection taskCollection, List<Type> callTypes, CancellationToken cancellationToken)
        {
            var startupFunctionExists = callTypes.Any(t => t.Name == Context.StartupFunctionName);
            if (startupFunctionExists)
            {
                return;
            }

            taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in AsyncLocals.NotificationService.GetNotifications(Context.StartupFunctionName).WithCancellation(cancellationToken))
                    {
                        if (notification is CallTriggerNotification callTriggerNotification)
                        {
                            var instance = await AsyncLocals.CacheService.GetCallInstance(callTriggerNotification.Call).ConfigureAwait(false);
                            await AsyncLocals.EnterContext(instance, callTriggerNotification.Call, async () =>
                            {
                                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
                                await AsyncLocals.NotificationService.ConsumeNotification(key).ConfigureAwait(false);
                            }).ConfigureAwait(false);
                        }
                    }
                });
        }

        public static void ListenCallNotifications(TaskCollection taskCollection, List<Type> callTypes, CancellationToken cancellationToken)
        {
            foreach (var callType in callTypes)
            {
                taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in AsyncLocals.NotificationService.GetNotifications(callType.Name).WithCancellation(cancellationToken))
                    {
                        if (notification is CallTriggerNotification callTriggerNotification)
                        {
                            taskCollection.Add(ExecuteCall(callType, key, callTriggerNotification));
                        }
                    }
                });
            }
        }

        public static void ListenStreamNotifications(TaskCollection taskCollection, List<Type> streamTypes, CancellationToken cancellationToken)
        {
            foreach (var streamType in streamTypes)
            {
                taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in AsyncLocals.NotificationService.GetNotifications(streamType.Name).WithCancellation(cancellationToken))
                    {
                        if (notification is StreamTriggerNotification streamTriggerNotification)
                        {
                            taskCollection.Add(ExecuteStream(streamType, key, streamTriggerNotification));
                        }
                    }
                });
            }
        }

        #endregion ListenNotifications
        #region Execute

        private static async Task ExecuteCall(Type callType, NotificationKey key, CallTriggerNotification notification)
        {
            var instance = await AsyncLocals.CacheService.GetCallInstance(notification.Call).ConfigureAwait(false);
            await AsyncLocals.EnterContext(instance, notification.Call, async () =>
            {
                var callInstance = InstanciateType(callType);

                var callArguments = await AsyncLocals.CacheService.GetCallParameters(AsyncLocals.Execution).ConfigureAwait(false);
                var (returnType, invokeResult) = await InvokeMethodAsync(callType, callInstance, callArguments).ConfigureAwait(false);

                await WriteCallResultAsync(key, returnType, invokeResult).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static async Task ExecuteStream(Type streamType, NotificationKey key, StreamTriggerNotification notification)
        {
            var instance = await AsyncLocals.CacheService.GetStreamInstance(notification.Stream).ConfigureAwait(false);
            await AsyncLocals.EnterContext(instance, notification.Stream, async () =>
            {
                var streamInstance = InstanciateType(streamType);

                var streamArguments = await AsyncLocals.CacheService.GetStreamParameters(AsyncLocals.Execution).ConfigureAwait(false);
                var (returnType, invokeResult) = await InvokeMethodAsync(streamType, streamInstance, streamArguments).ConfigureAwait(false);

                await WriteStreamResultAsync(key, returnType, invokeResult).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static object InstanciateType(Type callType)
        {
            // dependency injection
            var parametrizedConstructor = callType
            .GetConstructors()
            .FirstOrDefault(c => c.GetParameters().Length > 0)?
            .GetParameters();

            var constructorArguments = new object[parametrizedConstructor?.Length ?? 0];

            for (var i = 0 ; i < parametrizedConstructor?.Length ; i++)
            {
                var parameterInfo = parametrizedConstructor[i];

                if (parameterInfo.ParameterType == typeof(IContext))
                {
                    constructorArguments[i] = new Context();
                }
            }

            return Activator.CreateInstance(callType, constructorArguments)!;
        }

        private static async Task<(Type?, object?)> InvokeMethodAsync(Type type, object instance, object[] arguments)
        {
            var methodInfo = type.GetMethod(RunAsyncMethodName)!;
            var parameters = methodInfo.GetParameters();
            // if parameters.Count != arguments.Count -> exception

            var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            object? invokeResult = null;
            Type? returnType = null;

            // Pass CancellationToken
            if (isAwaitable)
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    returnType = methodInfo.ReturnType.GetGenericArguments()[0];
                    invokeResult = await (dynamic)methodInfo.Invoke(instance, arguments)!;
                }
                else
                {
                    await (dynamic)methodInfo.Invoke(instance, arguments)!;
                }

            }
            else
            {
                invokeResult = methodInfo.Invoke(instance, arguments);
                if (methodInfo.ReturnType != typeof(void))
                {
                    returnType = methodInfo.ReturnType;
                }
            }

            return (returnType, invokeResult);
        }

        private static async Task WriteCallResultAsync(NotificationKey key, Type? returnType, object? invokeResult)
        {
            if (returnType != null)
            {
                var callWriteResultMethod = typeof(CacheService).GetMethod(nameof(CacheService.CallWriteResult))!
                    .MakeGenericMethod(returnType);
                await ((Task)callWriteResultMethod.Invoke(AsyncLocals.CacheService, new object?[] { AsyncLocals.Execution, invokeResult })!).ConfigureAwait(false);
            }
            else
            {
                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
            }

            await AsyncLocals.NotificationService.ConsumeNotification(key).ConfigureAwait(false);
        }

        private static async Task WriteStreamResultAsync(NotificationKey key, Type? returnType, object? invokeResult)
        {
            if (returnType != null)
            {
                Type? asyncEnumerableType = null;
                try
                {
                    var isAsyncEnumerable = returnType.GetGenericTypeDefinition().Equals(typeof(IAsyncEnumerable<>));
                    if (isAsyncEnumerable)
                    {
                        asyncEnumerableType = returnType.GetGenericArguments()[0];
                    }
                }
                catch (InvalidOperationException)
                {
                }

                if (asyncEnumerableType != null)
                {
                    var processMethod = typeof(PerperStartup).GetMethod(nameof(ProcessAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(asyncEnumerableType);

                    await ((Task)processMethod.Invoke(null, new object?[] { invokeResult })!).ConfigureAwait(false);
                }
            }

            await AsyncLocals.NotificationService.ConsumeNotification(key).ConfigureAwait(false);
        }

        private static Type? GetGenericInterface(Type type, Type genericInterface)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericInterface)
            {
                return type;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericInterface)
                {
                    return iface;
                }
            }

            return null;
        }

        private static async Task ProcessAsyncEnumerable<T>(IAsyncEnumerable<T> values)
        {
            // TODO: CancellationToken
            await foreach (var value in values)
            {
                await AsyncLocals.CacheService.StreamWriteItem(AsyncLocals.Execution, value).ConfigureAwait(false);
            }
        }
        #endregion Execute
    }
}