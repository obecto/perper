using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        public static async Task EnterServicesContext(string agent, Func<Task> context, bool useInstance = false)
        {
            var (cacheService, notificationService) = await InitializeServices(agent, useInstance).ConfigureAwait(false);

            AsyncLocals.SetConnection(cacheService, notificationService);

            await context().ConfigureAwait(false);

            await notificationService.StopAsync().ConfigureAwait(false);
            notificationService.Dispose();
        }

        public static async Task<(CacheService, NotificationService)> InitializeServices(string agent, bool useInstance = false)
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

        public static Task ListenNotificationsForTypes((List<Type> streamTypes, List<Type> callTypes) types, CancellationToken cancellationToken)
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
                    await foreach (var (key, notification) in AsyncLocals.NotificationService.GetCallTriggerNotifications(Context.StartupFunctionName).ReadAllAsync(cancellationToken))
                    {
                        await AsyncLocals.EnterContext(notification.Instance, notification.Call, async () =>
                        {
                            await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
                            await AsyncLocals.NotificationService.ConsumeNotification(key).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                });
        }

        public static void ListenCallNotifications(TaskCollection taskCollection, List<Type> callTypes, CancellationToken cancellationToken)
        {
            foreach (var callType in callTypes)
            {
                taskCollection.Add(async () =>
                {
                    await foreach (var (key, notification) in AsyncLocals.NotificationService.GetCallTriggerNotifications(callType.Name).ReadAllAsync(cancellationToken))
                    {
                        taskCollection.Add(ExecuteCall(callType, key, notification));
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
                    await foreach (var (key, notification) in AsyncLocals.NotificationService.GetStreamTriggerNotifications(streamType.Name).ReadAllAsync(cancellationToken))
                    {
                        taskCollection.Add(ExecuteStream(streamType, key, notification));
                    }
                });
            }
        }

        #endregion ListenNotifications
        #region Execute

        private static async Task ExecuteCall(Type callType, NotificationKey key, CallTriggerNotification notification)
        {
            await AsyncLocals.EnterContext(notification.Instance, notification.Call, async () =>
            {
                try
                {
                    var callInstance = InstanciateType(callType);

                    var callArguments = await AsyncLocals.CacheService.GetCallParameters(AsyncLocals.Execution).ConfigureAwait(false);
                    var (returnType, invokeResult) = await InvokeMethodAsync(callType, callInstance, callArguments).ConfigureAwait(false);

                    await WriteCallResultAsync(key, returnType, invokeResult).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception while invoking call {notification.Call} ({callType}): {e}");
                    await WriteCallResultAsync(key, null, e).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }

        private static async Task ExecuteStream(Type streamType, NotificationKey key, StreamTriggerNotification notification)
        {
            await AsyncLocals.EnterContext(notification.Instance, notification.Stream, async () =>
            {
                try
                {
                    var streamInstance = InstanciateType(streamType);

                    var streamArguments = await AsyncLocals.CacheService.GetStreamParameters(AsyncLocals.Execution).ConfigureAwait(false);
                    var (returnType, invokeResult) = await InvokeMethodAsync(streamType, streamInstance, streamArguments).ConfigureAwait(false);

                    await WriteStreamResultAsync(key, returnType, invokeResult).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception while executing stream {notification.Stream} ({streamType}): {e}");
                    await WriteStreamResultAsync(key, null, e).ConfigureAwait(false);
                }
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
                else if (parameterInfo.ParameterType == typeof(IState))
                {
                    constructorArguments[i] = new State();
                }
            }

            return Activator.CreateInstance(callType, constructorArguments)!;
        }

        private static async Task<(Type?, object?)> InvokeMethodAsync(Type type, object instance, object[] arguments)
        {
            // System.Console.WriteLine($"Invoking {type}.{RunAsyncMethodName}`${arguments.Length}(${string.Join(", ", arguments.Select(x=>x.ToString()))})");
            var methodInfo = type.GetMethod(RunAsyncMethodName)!;
            var parameters = methodInfo.GetParameters();

            var castArguments = new object[parameters.Length];
            for (var i = 0 ; i < parameters.Length ; i++)
            {
                object castArgument;
                if (i < arguments.Length)
                {
                    var arg = arguments[i];

                    castArgument = arg != null && parameters[i].ParameterType.IsAssignableFrom(arg.GetType())
                        ? arg
                        : arg is ArrayList arrayList && parameters[i].ParameterType == typeof(object[])
                            ? arrayList.Cast<object>().ToArray()
                            : Convert.ChangeType(arg, parameters[i].ParameterType);
                }
                else
                {
                    if (!parameters[i].HasDefaultValue)
                    {
                        throw new ArgumentException($"Not enough arguments passed to {type}.{RunAsyncMethodName}; expected at least {i + 1}, got {arguments.Length}");
                    }
                    castArgument = parameters[i].DefaultValue;
                }
                castArguments[i] = castArgument;
            }

            var isAwaitable = methodInfo.ReturnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            object? invokeResult = null;
            Type? returnType = null;

            // Pass CancellationToken
            if (isAwaitable)
            {
                if (methodInfo.ReturnType.IsGenericType)
                {
                    returnType = methodInfo.ReturnType.GetGenericArguments()[0];
                    invokeResult = await (dynamic)methodInfo.Invoke(instance, castArguments)!;
                }
                else
                {
                    await (dynamic)methodInfo.Invoke(instance, castArguments)!;
                }

            }
            else
            {
                invokeResult = methodInfo.Invoke(instance, castArguments);
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
                object[] results;
                if (typeof(ITuple).IsAssignableFrom(returnType) && invokeResult is ITuple tuple)
                {
                    results = new object[tuple.Length];
                    for (var i = 0 ; i < results.Length ; i++)
                    {
                        results[i] = tuple[i];
                    }
                }
                else
                {
                    results = typeof(object[]) == returnType ? (object[])invokeResult : (new object[] { invokeResult });
                }

                await AsyncLocals.CacheService.CallWriteResult(AsyncLocals.Execution, results).ConfigureAwait(false);
            }
            else
            {
                if (invokeResult is Exception e)
                {
                    await AsyncLocals.CacheService.CallWriteError(AsyncLocals.Execution, e.Message).ConfigureAwait(false);
                }
                else
                {
                    await AsyncLocals.CacheService.CallWriteFinished(AsyncLocals.Execution).ConfigureAwait(false);
                }
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