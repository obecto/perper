using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Perper.Extensions;

namespace Perper.Application
{
    public static class PerperStartupExtensions
    {
        public const string RunMethodName = "Run";
        public const string RunAsyncMethodName = "RunAsync";
        public const string StreamsNamespaceName = "Streams";
        public const string CallsNamespaceName = "Calls";
        public const string InitTypeName = "Init";

        public static PerperStartup AddDiscoveredHandlers(this PerperStartup startup, Assembly? assembly = null, string? rootNamespace = null)
        {
            assembly ??= Assembly.GetEntryAssembly()!;
            rootNamespace ??= assembly.GetName().Name!;

            var streamsNamespace = $"{rootNamespace}.{StreamsNamespaceName}";
            var callsNamespace = $"{rootNamespace}.{CallsNamespaceName}";

            foreach (var type in assembly.GetTypes())
            {
                var runMethod = type.GetMethod(RunAsyncMethodName) ?? type.GetMethod(RunMethodName);
                if (type.IsClass && type.MemberType == MemberTypes.TypeInfo && !string.IsNullOrEmpty(type.Namespace) && runMethod != null)
                {
                    if (type.Namespace!.Contains(callsNamespace, StringComparison.InvariantCultureIgnoreCase))
                    {
                        startup = type.Name == InitTypeName ? startup.AddInitHandler(type, runMethod) : startup.AddCallHandler(type.Name, type, runMethod);
                    }
                    else if (type.Namespace!.Contains(streamsNamespace, StringComparison.InvariantCultureIgnoreCase))
                    {
                        startup = startup.AddStreamHandler(type.Name, type, runMethod);
                    }
                }
            }

            return startup;
        }

        public static PerperStartup AddStreamHandler<T>(this PerperStartup startup) => startup.AddStreamHandler(typeof(T));

        public static PerperStartup AddStreamHandler(this PerperStartup startup, Type streamType) =>
            startup.AddStreamHandler(streamType.Name, streamType, streamType.GetMethod(RunAsyncMethodName) ?? streamType.GetMethod(RunMethodName));

        public static PerperStartup AddStreamHandler(this PerperStartup startup, string @delegate, Type streamType, MethodInfo methodInfo)
        {
            return startup.AddStreamHandler(@delegate, () => ExecuteStream(streamType, methodInfo));
        }

        public static PerperStartup AddCallHandler<T>(this PerperStartup startup) => startup.AddCallHandler(typeof(T));

        public static PerperStartup AddCallHandler(this PerperStartup startup, Type callType) =>
            startup.AddCallHandler(callType.Name, callType, callType.GetMethod(RunAsyncMethodName) ?? callType.GetMethod(RunMethodName));

        public static PerperStartup AddCallHandler(this PerperStartup startup, string @delegate, Type callType, MethodInfo methodInfo)
        {
            return startup.AddCallHandler(@delegate, () => ExecuteCall(callType, methodInfo));
        }

        public static PerperStartup AddInitHandler<T>(this PerperStartup startup) => startup.AddInitHandler(typeof(T));

        public static PerperStartup AddInitHandler(this PerperStartup startup, Type initType) =>
            startup.AddInitHandler(initType, initType.GetMethod(RunAsyncMethodName) ?? initType.GetMethod(RunMethodName));

        public static PerperStartup AddInitHandler(this PerperStartup startup, Type initType, MethodInfo methodInfo)
        {
            return startup.AddInitHandler(() => ExecuteInit(initType, methodInfo));
        }

        #region Execute
        private static async Task ExecuteInit(Type callType, MethodInfo methodInfo)
        {
            try
            {
                var initArguments = Array.Empty<object>();
                await InvokeMethodAsync(callType, methodInfo, initArguments).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception while invoking init ({callType}.{methodInfo}): {e}");
            }
        }

        private static async Task ExecuteCall(Type callType, MethodInfo methodInfo)
        {
            try
            {
                var callArguments = await AsyncLocals.CacheService.GetCallParameters(AsyncLocals.Execution).ConfigureAwait(false);
                var (returnType, invokeResult) = await InvokeMethodAsync(callType, methodInfo, callArguments).ConfigureAwait(false);

                await WriteCallResultAsync(returnType, invokeResult).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception while invoking call {AsyncLocals.Execution} ({callType}.{methodInfo}): {e}");
                await WriteCallResultAsync(null, e).ConfigureAwait(false);
            }
        }

        private static async Task ExecuteStream(Type streamType, MethodInfo methodInfo)
        {
            try
            {
                var streamArguments = await AsyncLocals.CacheService.GetStreamParameters(AsyncLocals.Execution).ConfigureAwait(false);
                var (returnType, invokeResult) = await InvokeMethodAsync(streamType, methodInfo, streamArguments).ConfigureAwait(false);

                await WriteStreamResultAsync(returnType, invokeResult).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception while executing stream {AsyncLocals.Execution} ({streamType}.{methodInfo}): {e}");
                await WriteStreamResultAsync(null, e).ConfigureAwait(false);
            }
        }

        private static async Task<(Type?, object?)> InvokeMethodAsync(Type type, MethodInfo methodInfo, object[] arguments)
        {
            // System.Console.WriteLine($"Invoking {type}.{RunAsyncMethodName}`${arguments.Length}(${string.Join(", ", arguments.Select(x=>x.ToString()))})");
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

            var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);

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

        private static async Task WriteCallResultAsync(Type? returnType, object? invokeResult)
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
        }

        private static async Task WriteStreamResultAsync(Type? returnType, object? invokeResult)
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
                    var processMethod = typeof(PerperStartupExtensions).GetMethod(nameof(ProcessAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(asyncEnumerableType);

                    await ((Task)processMethod.Invoke(null, new object?[] { invokeResult })!).ConfigureAwait(false);
                }
            }
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