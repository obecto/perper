using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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

        public static PerperStartup DiscoverHandlersFromAssembly(this PerperStartup startup, Assembly? assembly = null, string? rootNamespace = null)
        {
            assembly ??= Assembly.GetEntryAssembly()!;
            rootNamespace ??= assembly.GetName().Name!;

            var streamsNamespace = $"{rootNamespace}.{StreamsNamespaceName}";
            var callsNamespace = $"{rootNamespace}.{CallsNamespaceName}";

            foreach (var type in assembly.GetTypes())
            {
                if (!type.IsClass || type.MemberType != MemberTypes.TypeInfo || string.IsNullOrEmpty(type.Namespace))
                {
                    continue;
                }

                var runMethod = GetRunMethod(type);
                if (runMethod is null)
                {
                    continue;
                }

                if (type.Namespace!.Contains(callsNamespace, StringComparison.InvariantCultureIgnoreCase))
                {
                    startup = type.Name == InitTypeName ? startup.AddInitHandler(type, runMethod) : startup.AddCallHandler(type.Name, type, runMethod);
                }
                else if (type.Namespace!.Contains(streamsNamespace, StringComparison.InvariantCultureIgnoreCase))
                {
                    startup = startup.AddStreamHandler(type.Name, type, runMethod);
                }
            }

            return startup;
        }

        public static PerperStartup AddInitHandler<T>(this PerperStartup startup) => startup.AddInitHandler(typeof(T));

        public static PerperStartup AddInitHandler(this PerperStartup startup, Type initType) =>
            startup.AddInitHandler(initType, GetRunMethodOrThrow(initType));

        public static PerperStartup AddInitHandler(this PerperStartup startup, Type initType, MethodInfo methodInfo)
        {
            return startup.AddInitHandler(() => HandleInit(initType, methodInfo));
        }

        public static PerperStartup AddCallHandler<T>(this PerperStartup startup) => startup.AddCallHandler(typeof(T));

        public static PerperStartup AddCallHandler(this PerperStartup startup, Type callType) =>
            startup.AddCallHandler(callType.Name, callType, GetRunMethodOrThrow(callType));

        public static PerperStartup AddCallHandler(this PerperStartup startup, string @delegate, Type callType, MethodInfo methodInfo)
        {
            return startup.AddCallHandler(@delegate, () => HandleCall(callType, methodInfo));
        }

        public static PerperStartup AddStreamHandler<T>(this PerperStartup startup) => startup.AddStreamHandler(typeof(T));

        public static PerperStartup AddStreamHandler(this PerperStartup startup, Type streamType) =>
            startup.AddStreamHandler(streamType.Name, streamType, GetRunMethodOrThrow(streamType));

        public static PerperStartup AddStreamHandler(this PerperStartup startup, string @delegate, Type streamType, MethodInfo methodInfo)
        {
            return startup.AddStreamHandler(@delegate, () => HandleStream(streamType, methodInfo));
        }

        private static MethodInfo? GetRunMethod(Type type)
        {
            return type.GetMethod(RunAsyncMethodName) ?? type.GetMethod(RunMethodName);
        }

        private static MethodInfo GetRunMethodOrThrow(Type type)
        {
            return GetRunMethod(type) ?? throw new ArgumentOutOfRangeException($"Type {type} does not contain a definition for {RunAsyncMethodName} or {RunMethodName}");
        }

        #region Execute
        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        private static async Task HandleInit(Type callType, MethodInfo methodInfo)
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

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        private static async Task HandleCall(Type callType, MethodInfo methodInfo)
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

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        private static async Task HandleStream(Type streamType, MethodInfo methodInfo)
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

        private static object? CastArgument(object? arg, Type parameterType)
        {
            return arg != null && parameterType.IsAssignableFrom(arg.GetType())
                ? arg
                : arg is ArrayList arrayList && parameterType == typeof(object[])
                    ? arrayList.Cast<object>().ToArray()
                    : Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);
        }

        private static async Task<(Type?, object?)> InvokeMethodAsync(Type type, MethodInfo methodInfo, object?[] arguments)
        {
            // System.Console.WriteLine($"Invoking {type}.{RunAsyncMethodName}`${arguments.Length}(${string.Join(", ", arguments.Select(x=>x.ToString()))})");
            var parameters = methodInfo.GetParameters();

            var castArguments = new object?[parameters.Length];
            for (var i = 0 ; i < parameters.Length ; i++)
            {
                object? castArgument;
                if (i == parameters.Length - 1 && parameters[i].GetCustomAttribute<ParamArrayAttribute>() != null)
                {
                    var paramsType = parameters[i].ParameterType.GetElementType()!;
                    if (i < arguments.Length)
                    {
                        var paramsArray = Array.CreateInstance(paramsType, arguments.Length - i);
                        for (var j = 0 ; j < paramsArray.Length ; j++)
                        {
                            paramsArray.SetValue(CastArgument(arguments[i + j], paramsType), j);
                        }
                        castArgument = paramsArray;
                    }
                    else
                    {
                        castArgument = Array.CreateInstance(paramsType, 0);
                    }

                }
                else if (i < arguments.Length)
                {
                    castArgument = CastArgument(arguments[i], parameters[i].ParameterType);
                }
                else
                {
                    if (!parameters[i].HasDefaultValue)
                    {
                        throw new ArgumentException($"Not enough arguments passed to {type}.{methodInfo}; expected at least {i + 1}, got {arguments.Length}");
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
                object?[] results;
                if (typeof(ITuple).IsAssignableFrom(returnType) && invokeResult is ITuple tuple)
                {
                    results = new object?[tuple.Length];
                    for (var i = 0 ; i < results.Length ; i++)
                    {
                        results[i] = tuple[i];
                    }
                }
                else
                {
                    results = typeof(object[]) == returnType ? (object?[])invokeResult! : (new object?[] { invokeResult });
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
            var key = 0L;
            // TODO: CancellationToken
            await foreach (var value in values)
            {
                await AsyncLocals.CacheService.StreamWriteItem(AsyncLocals.Execution, key, value).ConfigureAwait(false);
                key++;
            }
        }
        #endregion Execute
    }
}