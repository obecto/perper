namespace Perper.Application
{
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

    public static class PerperStartup
    {
        private static CacheService cacheService;
        private static NotificationService notificationService;
        private static TaskCollection taskCollection;
        private const string runAsyncMethodName = "RunAsync";
        private const string initCallName = "Init";

        public static async Task RunAsync(string agent, CancellationToken cancellationToken)
        {
            InitializeServices(agent);

            var (streamTypes, callTypes) = DiscoverStreamAndCallTypes();
            var initCallType = callTypes.FirstOrDefault(c => c.Name == initCallName);

            RemoveInitCallFromTypes(callTypes, initCallType);

            ListenCallNotifications(callTypes);
            ListenStreamNotifications(streamTypes);

            await notificationService.StartAsync().ConfigureAwait(false);
            await InvokeInitMethodIfExists(initCallType).ConfigureAwait(false);

            await taskCollection.GetTask().ConfigureAwait(false);
            WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle });
            await notificationService.StopAsync().ConfigureAwait(false);
        }

        private static void InitializeServices(string agent)
        {
            var ignite = Ignition.StartClient(new IgniteClientConfiguration
            {
                Endpoints = new List<string> { "127.0.0.1:10800" },
                BinaryConfiguration = new BinaryConfiguration
                {
                    NameMapper = PerperBinaryConfigurations.NameMapper,
                    TypeConfigurations = PerperBinaryConfigurations.TypeConfigurations
                }

            });
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            var grpcChannel = GrpcChannel.ForAddress("http://127.0.0.1:40400");

            taskCollection = new TaskCollection();
            cacheService = new CacheService(ignite);
            notificationService = new NotificationService(ignite, grpcChannel, agent);

            AsyncLocals.SetConnection(cacheService, notificationService);
        }

        private static async Task InvokeInitMethodIfExists(Type? initCallType)
        {
            if (initCallType != null)
            {
                await InvokeInitMethod(initCallType, "test-instance").ConfigureAwait(false);
            }
        }

        private static void RemoveInitCallFromTypes(List<Type>? callTypes, Type? initCallType)
        {
            if (initCallType != null)
            {
                callTypes.Remove(initCallType);
            }
        }

        /// <summary>
        /// Get all stream and call types in "Assembly.Streams" and "Assembly.Calls" namespaces
        /// </summary>
        /// <returns>(streamTypes, callTypes)</returns>
        private static (List<Type>, List<Type>) DiscoverStreamAndCallTypes()
        {
            var assembly = Assembly.GetEntryAssembly();

            var assemblyName = assembly.GetName().Name;
            var streamsNamespace = $"{assemblyName}.Streams";
            var callsNamespace = $"{assemblyName}.Calls";

            var types = assembly
                .GetTypes()
                .Where(t => t.IsClass && t.MemberType == MemberTypes.TypeInfo && !string.IsNullOrEmpty(t.Namespace));

            var streamTypes = types
                 .Where(x => x.Namespace.Contains(streamsNamespace, StringComparison.InvariantCultureIgnoreCase))
                 .ToList();

            var callTypes = types
                 .Where(x => x.Namespace.Contains(callsNamespace, StringComparison.InvariantCultureIgnoreCase))
                 .ToList();

            return (streamTypes, callTypes);
        }

        private static Task InvokeInitMethod(Type initType, string instanceName)
        {
            return AsyncLocals.EnterContext(instanceName, async () =>
            {
                var initCallInstance = InstanciateType(initType);

                var methodInfo = initType.GetMethod(runAsyncMethodName);
                var parameters = methodInfo.GetParameters();

                // TODO: Pass start arguments
                var arguments = Array.Empty<object>();

                // if parameters.Count != arguments.Count -> exception

                var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;

                // Pass CancellationToken
                if (isAwaitable)
                {
                    //await (Task)methodInfo.Invoke(initCallInstance, arguments);
                    taskCollection.Add((Task)methodInfo.Invoke(initCallInstance, arguments));
                }
                else if (methodInfo.ReturnType == typeof(void))
                {
                    methodInfo.Invoke(initCallInstance, arguments);
                }
            });
        }

        private static void ListenCallNotifications(List<Type>? callTypes)
        {
            foreach (var callType in callTypes)
            {
                taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in notificationService.GetNotifications(callType.Name))
                    {
                        if (notification is CallTriggerNotification callTriggerNotification)
                        {
                            taskCollection.Add(ExecuteCall(callType, key, callTriggerNotification));
                        }
                    }
                });
            }
        }

        private static void ListenStreamNotifications(List<Type>? streamTypes)
        {
            foreach (var streamType in streamTypes)
            {
                taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in notificationService.GetNotifications(streamType.Name))
                    {
                        if (notification is StreamTriggerNotification streamTriggerNotification)
                        {
                            taskCollection.Add(ExecuteStream(streamType, key, streamTriggerNotification));
                        }
                    }
                });
            }
        }

        private static Task ExecuteCall(Type? callType, NotificationKey key, CallTriggerNotification notification)
        {
            return AsyncLocals.EnterContext(notification.Call, async () =>
            {
                var callInstance = InstanciateType(callType);

                var callArguments = await AsyncLocals.CacheService.GetCallParameters(AsyncLocals.Instance).ConfigureAwait(false);
                var (hasReturnType, invokeResult, _) = await InvokeMethodAsync(callType, callInstance, callArguments).ConfigureAwait(false);

                await WriteCallResultAsync(key, hasReturnType, invokeResult).ConfigureAwait(false);
            });
        }

        private static Task ExecuteStream(Type? streamType, NotificationKey key, StreamTriggerNotification notification)
        {
            return AsyncLocals.EnterContext(notification.Stream, async () =>
            {
                var streamInstance = InstanciateType(streamType);

                var streamArguments = await AsyncLocals.CacheService.GetStreamParameters(AsyncLocals.Instance).ConfigureAwait(false);
                var (hasReturnType, invokeResult, asyncEnumerableType) = await InvokeMethodAsync(streamType, streamInstance, streamArguments).ConfigureAwait(false);

                await WriteStreamResultAsync(key, hasReturnType, invokeResult, asyncEnumerableType).ConfigureAwait(false);
            });
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

            return Activator.CreateInstance(callType, constructorArguments);
        }

        private static async Task<(bool, object, Type?)> InvokeMethodAsync(Type type, object instance, object[] arguments)
        {
            var methodInfo = type.GetMethod(runAsyncMethodName);
            var parameters = methodInfo.GetParameters();
            // if parameters.Count != arguments.Count -> exception

            var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            object invokeResult = null;
            var hasReturnType = false;

            // Pass CancellationToken
            if (isAwaitable)
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    hasReturnType = true;
                    invokeResult = (object)await (dynamic)methodInfo.Invoke(instance, arguments);
                }
                else
                {
                    await ((Task)methodInfo.Invoke(instance, arguments)).ConfigureAwait(false);
                }
            }
            else
            {
                if (methodInfo.ReturnType == typeof(void))
                {
                    methodInfo.Invoke(instance, arguments);
                }
                else
                {
                    hasReturnType = true;
                    invokeResult = methodInfo.Invoke(instance, arguments);
                }
            }

            Type? asyncEnumerableType = null;

            try
            {
                var isAsyncEnumerable = methodInfo.ReturnType.GetGenericTypeDefinition().Equals(typeof(IAsyncEnumerable<>));
                if (isAsyncEnumerable)
                {
                    asyncEnumerableType = methodInfo.ReturnType.GetGenericArguments()[0];
                }
            }
            catch (InvalidOperationException)
            {
            }

            return (hasReturnType, invokeResult, asyncEnumerableType);
        }

        private static async Task WriteCallResultAsync(NotificationKey key, bool hasReturnType, object? invokeResult)
        {
            if (hasReturnType)
            {
                await AsyncLocals.CacheService.CallWriteResult(AsyncLocals.Instance, invokeResult).ConfigureAwait(false);
            }
            else
            {
                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Instance).ConfigureAwait(false);
            }

            await notificationService.ConsumeNotification(key).ConfigureAwait(false);
        }

        private static async Task WriteStreamResultAsync(NotificationKey key, bool hasReturnType, object? invokeResult, Type? asyncEnumerableType)
        {
            if (hasReturnType)
            {
                if (asyncEnumerableType != null)
                {
                    var processMethod = typeof(PerperStartup).GetMethod(nameof(ProcessAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(asyncEnumerableType);

                    await ((Task)processMethod.Invoke(null, new object[] { invokeResult })!).ConfigureAwait(false);
                }
                else
                {
                    await AsyncLocals.CacheService.CallWriteResult(AsyncLocals.Instance, invokeResult).ConfigureAwait(false);
                }
            }
            else
            {
                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Instance).ConfigureAwait(false);
                await notificationService.ConsumeNotification(key).ConfigureAwait(false);
            }
        }

        public static Type? GetGenericInterface(Type type, Type genericInterface)
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
                await AsyncLocals.CacheService.StreamWriteItem(AsyncLocals.Instance, value).ConfigureAwait(false);
            }
        }
    }
}
