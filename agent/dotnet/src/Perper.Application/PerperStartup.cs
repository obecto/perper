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

using Polly;

using Perper.Model;
using Perper.Protocol;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Service;
using Context = Perper.Model.Context;

namespace Perper.Application
{
    public static class PerperStartup
    {
        private static CacheService cacheService;
        private static NotificationService notificationService;
        private static TaskCollection taskCollection;
        private const string runAsyncMethodName = "RunAsync";
        private const string initCallName = "Init";

        public static async Task RunAsync(string agent, CancellationToken cancellationToken)
        {
            await InitializeServices(agent).ConfigureAwait(false);
            AsyncLocals.SetConnection(cacheService, notificationService);

            var (streamTypes, callTypes) = DiscoverStreamAndCallTypes();
            var initCallType = callTypes.FirstOrDefault(c => c.Name == initCallName);

            RemoveInitCallFromTypes(callTypes, initCallType);

            ListenCallNotifications(callTypes);
            ListenStreamNotifications(streamTypes);

            AcknowledgeStartupCalls(callTypes);

            await InvokeInitMethodIfExists(initCallType).ConfigureAwait(false);

            await taskCollection.GetTask().ConfigureAwait(false);
            WaitHandle.WaitAny(new[] { cancellationToken.WaitHandle });
            await notificationService.StopAsync().ConfigureAwait(false);
        }

        private static void AcknowledgeStartupCalls(List<Type>? callTypes)
        {
            var startupFunctionExists = callTypes.Any(t => t.Name == Context.StartupFunctionName);
            if (startupFunctionExists)
            {
                return;
            }

            taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in notificationService.GetNotifications(Context.StartupFunctionName))
                    {
                        if (notification is CallTriggerNotification callTriggerNotification)
                        {
                            var instance = await AsyncLocals.CacheService.GetCallInstance(callTriggerNotification.Call).ConfigureAwait(false);
                            await AsyncLocals.EnterContext(instance, callTriggerNotification.Call, async () =>
                            {
                                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
                                await notificationService.ConsumeNotification(key).ConfigureAwait(false);
                            }).ConfigureAwait(false);
                        }
                    }
                });

            //taskCollection.Add(AsyncLocals.EnterContext(Context.StartupFunctionName, async () =>
            //    {
            //        await foreach (var (key, notification) in notificationService.GetNotifications(Context.StartupFunctionName))
            //        {
            //            if (notification is CallTriggerNotification callTriggerNotification)
            //            {
            //                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
            //                await notificationService.ConsumeNotification(key).ConfigureAwait(false);
            //            }
            //        }
            //    }));
        }

        private static async Task InitializeServices(string agent)
        {
            taskCollection = new TaskCollection();

            var apacheIgniteEndpoint = Environment.GetEnvironmentVariable("APACHE_IGNITE_ENDPOINT") ?? "127.0.0.1:10800";
            var fabricGrpcAddress = Environment.GetEnvironmentVariable("PERPER_FABRIC_ENDPOINT") ?? "http://127.0.0.1:40400";

            Console.WriteLine($"APACHE_IGNITE_ENDPOINT: {apacheIgniteEndpoint}");
            Console.WriteLine($"PERPER_FABRIC_ENDPOINT: {fabricGrpcAddress}");

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var igniteConfiguration = new IgniteClientConfiguration
            {
                Endpoints = new List<string> { apacheIgniteEndpoint },
                BinaryConfiguration = new BinaryConfiguration
                {
                    NameMapper = PerperBinaryConfigurations.NameMapper,
                    TypeConfigurations = PerperBinaryConfigurations.TypeConfigurations
                }
            };

            var ignite = await Policy
                .HandleInner<System.Net.Sockets.SocketException>()
                .WaitAndRetryAsync(10,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)),
                    (exception, timespan) => Console.WriteLine("Failed to connect to Ignite, retrying in {0}s", timespan.TotalSeconds))
                .ExecuteAsync(() => Task.Run(() => Ignition.StartClient(igniteConfiguration))).ConfigureAwait(false);

            cacheService = new CacheService(ignite);

            var grpcChannel = GrpcChannel.ForAddress(fabricGrpcAddress);
            notificationService = new NotificationService(ignite, grpcChannel, agent);

            await Policy
                .Handle<Grpc.Core.RpcException>(ex => ex.Status.DebugException is System.Net.Http.HttpRequestException)
                .WaitAndRetryAsync(10,
                    attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 2)),
                    (exception, timespan) => Console.WriteLine("Failed to connect to GRPC, retrying in {0}s", timespan.TotalSeconds))
                .ExecuteAsync(notificationService.StartAsync).ConfigureAwait(false);
        }

        private static async Task InvokeInitMethodIfExists(Type? initCallType)
        {
            if (initCallType != null)
            {
                await InvokeInitMethod(initCallType).ConfigureAwait(false);
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

        private static Task InvokeInitMethod(Type initType)
        {
            return AsyncLocals.EnterContext($"{AsyncLocals.Agent}-init", $"{initType.Name}-init", async () =>
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

        private static async Task ExecuteCall(Type? callType, NotificationKey key, CallTriggerNotification notification)
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

        private static async Task ExecuteStream(Type? streamType, NotificationKey key, StreamTriggerNotification notification)
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

            return Activator.CreateInstance(callType, constructorArguments);
        }

        private static async Task<(Type?, object)> InvokeMethodAsync(Type type, object instance, object[] arguments)
        {
            var methodInfo = type.GetMethod(runAsyncMethodName);
            var parameters = methodInfo.GetParameters();
            // if parameters.Count != arguments.Count -> exception

            var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            object invokeResult = null;
            Type? returnType = null;

            // Pass CancellationToken
            if (isAwaitable)
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    returnType = methodInfo.ReturnType.GetGenericArguments()[0];
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
                    returnType = methodInfo.ReturnType;
                    invokeResult = methodInfo.Invoke(instance, arguments);
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
                await ((Task)callWriteResultMethod.Invoke(AsyncLocals.CacheService, new object[] { AsyncLocals.Execution, invokeResult })).ConfigureAwait(false);
            }
            else
            {
                await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
            }

            await notificationService.ConsumeNotification(key).ConfigureAwait(false);
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

                    await ((Task)processMethod.Invoke(null, new object[] { invokeResult })!).ConfigureAwait(false);
                }
            }

            await notificationService.ConsumeNotification(key).ConfigureAwait(false);
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
                await AsyncLocals.CacheService.StreamWriteItem(AsyncLocals.Execution, value).ConfigureAwait(false);
            }
        }
    }
}