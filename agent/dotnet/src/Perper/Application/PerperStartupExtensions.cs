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

                if (type.Namespace!.Contains(rootNamespace, StringComparison.InvariantCultureIgnoreCase))
                {
                    startup = type.Name == InitTypeName ? startup.AddInitHandler(type, runMethod) : startup.AddHandler(type.Name, type, runMethod);
                }
            }

            return startup;
        }

        public static PerperStartup AddInitHandler<T>(this PerperStartup startup) => startup.AddInitHandler(typeof(T));

        public static PerperStartup AddInitHandler(this PerperStartup startup, Type initType) =>
            startup.AddInitHandler(initType, GetRunMethodOrThrow(initType));

        public static PerperStartup AddInitHandler(this PerperStartup startup, Type initType, MethodInfo methodInfo)
        {
            return startup.AddInitHandler(CreateHandler(initType, methodInfo, isInit: true));
        }

        public static PerperStartup AddHandler<T>(this PerperStartup startup) => startup.AddHandler(typeof(T));

        public static PerperStartup AddHandler(this PerperStartup startup, Type type) =>
            startup.AddHandler(type.Name, type, GetRunMethodOrThrow(type));

        public static PerperStartup AddHandler(this PerperStartup startup, string @delegate, Type type, MethodInfo methodInfo)
        {
            return startup.AddHandler(@delegate, CreateHandler(type, methodInfo, isInit: false));
        }

        #region Utils
        private static MethodInfo? GetRunMethod(Type type)
        {
            return type.GetMethod(RunAsyncMethodName) ?? type.GetMethod(RunMethodName);
        }

        private static MethodInfo GetRunMethodOrThrow(Type type)
        {
            return GetRunMethod(type) ?? throw new ArgumentOutOfRangeException($"Type {type} does not contain a definition for {RunAsyncMethodName} or {RunMethodName}");
        }
        #endregion Utils

        #region Cast
        private static object?[] CastArguments(MethodInfo methodInfo, object?[] arguments)
        {
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
                        throw new ArgumentException($"Not enough arguments passed; expected at least {i + 1}, got {arguments.Length}");
                    }
                    castArgument = parameters[i].DefaultValue;
                }
                castArguments[i] = castArgument;
            }

            return castArguments;
        }

        private static object? CastArgument(object? arg, Type parameterType)
        {
            return arg != null && parameterType.IsAssignableFrom(arg.GetType())
                ? arg
                : arg is ArrayList arrayList && parameterType == typeof(object[])
                    ? arrayList.Cast<object>().ToArray()
                    : Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);
        }

        private static object?[]? CastResult(Type resultType, object? result)
        {
            if (typeof(ITuple).IsAssignableFrom(resultType))
            {
                if (result is ITuple tuple)
                {
                    var results = new object?[tuple.Length];
                    for (var i = 0 ; i < results.Length ; i++)
                    {
                        results[i] = tuple[i];
                    }
                    return results;
                }
                else
                {
                    return null;
                }
            }
            else if (resultType == typeof(object[]) || resultType == typeof(void))
            {
                return (object?[]?)result;
            }
            else
            {
                return new object?[] { result };
            }
        }
        #endregion Cast

        #region Execute
        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        private static Func<Task> CreateHandler(Type type, MethodInfo methodInfo, bool isInit)
        {
            // Pass CancellationToken?
            Func<ValueTask<object?>> invoker = async () =>
            {
                var arguments = isInit ? Array.Empty<object?>() : await AsyncLocals.FabricService.ReadExecutionParameters(AsyncLocals.Execution).ConfigureAwait(false);
                var castArguments = CastArguments(methodInfo, arguments);
                return methodInfo.Invoke(methodInfo.IsStatic ? null : Activator.CreateInstance(type), castArguments);
            };
            var returnType = methodInfo.ReturnType;

            var isAwaitable = returnType.GetMethod(nameof(Task.GetAwaiter)) != null;
            if (isAwaitable)
            {
                if (returnType.IsGenericType)
                {
                    var _invoker = invoker;
                    invoker = async () => await (dynamic)(await _invoker().ConfigureAwait(false))!;
                    returnType = returnType.GetGenericArguments()[0];
                }
                else
                {
                    var _invoker = invoker;
                    invoker = async () =>
                    {
                        await (dynamic)(await _invoker().ConfigureAwait(false))!;
                        return null;
                    };
                    returnType = typeof(void);
                }
            }

            var isStream = returnType.IsGenericType && returnType.GetGenericTypeDefinition().Equals(typeof(IAsyncEnumerable<>));
            if (isStream)
            {
                var itemType = returnType.GetGenericArguments()[0]!;
                MethodInfo processMethod;
                if (itemType.IsGenericType && itemType.GetGenericTypeDefinition().Equals(typeof(ValueTuple<,>)) && itemType.GetGenericArguments()[0].Equals(typeof(long)))
                {
                    itemType = itemType.GetGenericArguments()[1];
                    processMethod = typeof(PerperStartupExtensions).GetMethod(nameof(ProcessKeyedAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(itemType);
                }
                else
                {
                    processMethod = typeof(PerperStartupExtensions).GetMethod(nameof(ProcessAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Static)!
                        .MakeGenericMethod(itemType);
                }

                var _invoker = invoker;
                invoker = async () =>
                {
                    await AsyncLocals.FabricService.WaitListenerAttached(AsyncLocals.Execution, AsyncLocals.CancellationToken).ConfigureAwait(false);
                    var asyncEnumerable = await _invoker().ConfigureAwait(false);
                    await ((Task)processMethod.Invoke(null, new object?[] { asyncEnumerable })!).ConfigureAwait(false);
                    return null;
                };
                returnType = typeof(void);
            }

            return isInit ? async () => await invoker().ConfigureAwait(false) : async () =>
            {
                try
                {
                    var results = CastResult(returnType, await invoker().ConfigureAwait(false));
                    if (results == null)
                    {
                        await AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution).ConfigureAwait(false);
                    }
                    else
                    {
                        await AsyncLocals.FabricService.WriteExecutionResult(AsyncLocals.Execution, results).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception while executing {AsyncLocals.Execution}: {e}");
                    try
                    {
                        await AsyncLocals.FabricService.WriteExecutionException(AsyncLocals.Execution, e).ConfigureAwait(false);
                    }
                    catch (Exception e2)
                    {
                        Console.WriteLine($"Exception while executing {AsyncLocals.Execution}: {e2}");
                    }
                }
            };
        }

        private static async Task ProcessAsyncEnumerable<T>(IAsyncEnumerable<T> values)
        {
            await foreach (var value in values)
            {
                await AsyncLocals.FabricService.WriteStreamItem(AsyncLocals.Execution, value).ConfigureAwait(false);
            }
        }

        private static async Task ProcessKeyedAsyncEnumerable<T>(IAsyncEnumerable<(long, T)> values)
        {
            await foreach (var (key, value) in values)
            {
                await AsyncLocals.FabricService.WriteStreamItem(AsyncLocals.Execution, key, value).ConfigureAwait(false);
            }
        }
        #endregion Execute
    }
}