namespace Perper.Application
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Apache.Ignite.Core;
    using Apache.Ignite.Core.Binary;
    using Apache.Ignite.Core.Client;
    using Grpc.Net.Client;
    using Perper.Model;
    using Perper.Protocol;
    using Perper.Protocol.Cache.Notifications;
    using Perper.Protocol.Cache.Standard;
    using Perper.Protocol.Service;

    public static class Initializer
    {
        private static CacheService cacheService;
        private static NotificationService notificationService;
        private static TaskCollection taskCollection;
        private const string runAsyncMethodName = "RunAsync";
        private const string initCallName = "Init";

        private static Dictionary<string, Channel<(NotificationKey, Notification)>> callChannels;
        private static Dictionary<string, Channel<(NotificationKey, Notification)>> streamChannels;

        public static async Task RunAsync(string agent, CancellationToken cancellationToken)
        {
            InitializeServices(agent);

            #region Launcher

            taskCollection.Add(AsyncLocals.EnterContext("testInstance", async () =>
            {
                var context = new Context();

                var randomNumber = await context.CallFunctionAsync<int>("GetRandomNumber", new object[] { 1, 100 });
                var anotherRandomNumber = await context.CallFunctionAsync<int>("AnotherRandomNumber", new object[] { 1, 100 });
                var randomNumbersCollection = await context.CallFunctionAsync<List<int>>("GetRandomNumbersArray", new object[] { 1, 100 });

                await context.CallActionAsync("DoSomething", new object[] { "123" });
                await context.CallActionAsync("DoSomethingAsync", new object[] { "456" });
                await context.CallActionAsync("AddStrings", new object[] { new List<string> { "11", "22", "33", "44" } });
            }));

            //taskCollection.Add(AsyncLocals.EnterContext("testInstance", async () =>
            //{
            //    var context = new Context();

            //    const int messagesCount = 28;
            //    const int batchCount = 10;

            //    var generator = await context.StreamFunctionAsync<string>("Generator", new object[] { messagesCount });
            //    var processor = await context.StreamFunctionAsync<string[]>("Processor", new object[] { generator, batchCount });
            //    var consumer = await context.StreamActionAsync("Consumer", new object[] { processor });
            //}));

            #endregion Launcher

            var (streamTypes, callTypes) = DiscoverStreamAndCallTypes();

            //var initCall = callTypes.FirstOrDefault(c => c.Name == initCallName);
            // Invoke Init call first if exists

            ListenCallNotifications(callTypes, cancellationToken);
            ListenStreamNotifications(streamTypes, cancellationToken);

            ExecuteCalls(callTypes);
            ExecuteStreams(streamTypes);

            await notificationService.StartAsync();
            WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle });
            await notificationService.StopAsync();
        }

        private static void ExecuteStreams(List<Type>? streamTypes)
        {
            foreach (var streamType in streamTypes)
            {
                taskCollection.Add(async () =>
                {
                    var streamChannel = GetOrAddChannel(streamChannels, streamType.Name);
                    await foreach (var (key, notification) in streamChannel.Reader.ReadAllAsync())
                    {
                        if (notification is StreamTriggerNotification streamTriggerNotification)
                        {
                            taskCollection.Add(ExecuteStream(streamType, key, streamTriggerNotification));
                        }
                    }
                });
            }
        }

        private static void ExecuteCalls(List<Type>? callTypes)
        {
            foreach (var callType in callTypes)
            {
                taskCollection.Add(async () =>
                {
                    var callChannel = GetOrAddChannel(callChannels, callType.Name);
                    await foreach (var (key, notification) in callChannel.Reader.ReadAllAsync())
                    {
                        if (notification is CallTriggerNotification callTriggerNotification)
                        {
                            taskCollection.Add(ExecuteCall(callType, key, callTriggerNotification));
                        }
                    }
                });
            }
        }

        private static Channel<(NotificationKey, Notification)> GetOrAddChannel(Dictionary<string, Channel<(NotificationKey, Notification)>> dictionary, string key)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, Channel.CreateUnbounded<(NotificationKey, Notification)>());
            }

            return dictionary[key];
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

            callChannels = new Dictionary<string, Channel<(NotificationKey, Notification)>>();
            streamChannels = new Dictionary<string, Channel<(NotificationKey, Notification)>>();

            AsyncLocals.SetConnection(cacheService, notificationService);
        }

        /// <summary>
        /// Get all stream and call types in "Assembly.Streams" and "Assembly.Calls" namespaces
        /// </summary>
        /// <returns>(streamTypes, callTypes)</returns>
        private static (List<Type>, List<Type>) DiscoverStreamAndCallTypes()
        {
            var assembly = Assembly.GetEntryAssembly();

            var assemblyName = assembly.GetName().Name;
            string streamsNamespace = $"{assemblyName}.Streams";
            string callsNamespace = $"{assemblyName}.Calls";

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

        private static void ListenCallNotifications(List<Type>? callTypes, CancellationToken cancellationToken)
        {
            foreach (var callType in callTypes)
            {
                taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in notificationService.GetNotifications(callType.Name))
                    {
                        await AddNotificationToChannelAsync(callChannels, callType.Name, key, notification);
                    }
                });
            }
        }

        private static void ListenStreamNotifications(List<Type>? streamTypes, CancellationToken cancellationToken)
        {
            foreach (var streamType in streamTypes)
            {
                taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in notificationService.GetNotifications(streamType.Name))
                    {
                        await AddNotificationToChannelAsync(streamChannels, streamType.Name, key, notification);
                    }
                });
            }
        }

        private static async Task AddNotificationToChannelAsync(Dictionary<string, Channel<(NotificationKey, Notification)>> dictionary, string key, NotificationKey notificationKey, Notification notification)
        {
            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, Channel.CreateUnbounded<(NotificationKey, Notification)>());
            }

            await dictionary[key].Writer.WriteAsync((notificationKey, notification));
        }

        private static Task ExecuteCall(Type? callType, NotificationKey key, CallTriggerNotification notification)
        {
            return AsyncLocals.EnterContext(notification.Call, async () =>
            {
                object callInstance = InstanciateType(callType);

                var callArguments = await AsyncLocals.CacheService.GetCallParameters(AsyncLocals.Instance);
                var (hasReturnType, invokeResult, _) = await InvokeMethodAsync(callType, callInstance, callArguments);

                await WriteCallResultAsync(key, hasReturnType, invokeResult);
            });
        }

        private static Task ExecuteStream(Type? streamType, NotificationKey key, StreamTriggerNotification notification)
        {
            return AsyncLocals.EnterContext(notification.Stream, async () =>
            {
                object streamInstance = InstanciateType(streamType);

                var streamArguments = await AsyncLocals.CacheService.GetStreamParameters(AsyncLocals.Instance);
                var (hasReturnType, invokeResult, isAsyncEnumerable) = await InvokeMethodAsync(streamType, streamInstance, streamArguments);

                await WriteStreamResultAsync(key, hasReturnType, invokeResult, isAsyncEnumerable);
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

            for (int i = 0; i < parametrizedConstructor?.Length; i++)
            {
                var parameterInfo = parametrizedConstructor[i];

                if (parameterInfo.ParameterType == typeof(IContext))
                {
                    constructorArguments[i] = new Context();
                }
            }

            return Activator.CreateInstance(callType, constructorArguments);
        }

        private static async Task<(bool, object, bool)> InvokeMethodAsync(Type type, object instance, object[] arguments)
        {
            MethodInfo methodInfo = type.GetMethod(runAsyncMethodName);
            var parameters = methodInfo.GetParameters();
            // if parameters.Count != arguments.Count -> exception

            var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            object invokeResult = null;
            bool hasReturnType = false;

            if (isAwaitable)
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    hasReturnType = true;
                    invokeResult = (object)await (dynamic)methodInfo.Invoke(instance, arguments);
                }
                else
                {
                    await (Task)methodInfo.Invoke(instance, arguments);
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

            bool isAsyncEnumerable;

            try
            {
                isAsyncEnumerable = methodInfo.ReturnType.GetGenericTypeDefinition().Equals(typeof(IAsyncEnumerable<>));
            }
            catch (InvalidOperationException)
            {
                isAsyncEnumerable = false;
            }

            return (hasReturnType, invokeResult, isAsyncEnumerable);
        }

        private static async Task WriteCallResultAsync(NotificationKey key, bool hasReturnType, object? invokeResult)
        {
            if (hasReturnType)
            {
                await AsyncLocals.CacheService.CallWriteResult(AsyncLocals.Instance, invokeResult);
            }
            else
            {
                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Instance);
            }

            await notificationService.ConsumeNotification(key);
        }

        private static async Task WriteStreamResultAsync(NotificationKey key, bool hasReturnType, object? invokeResult, bool isAsyncEnumerable)
        {
            if (hasReturnType)
            {
                if (isAsyncEnumerable)
                {
                    var asyncEnumerable = (IAsyncEnumerable<object>)invokeResult;
                    await notificationService.ConsumeNotification(key);

                    await foreach (var item in asyncEnumerable)
                    {
                        await AsyncLocals.CacheService.StreamWriteItem(AsyncLocals.Instance, item);
                    }
                }
                else
                {
                    await AsyncLocals.CacheService.CallWriteResult(AsyncLocals.Instance, invokeResult);
                }
            }
            else
            {
                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Instance);
                await notificationService.ConsumeNotification(key);
            }
        }
    }
}
