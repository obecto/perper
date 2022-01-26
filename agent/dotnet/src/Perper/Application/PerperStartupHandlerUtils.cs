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
    public static class PerperStartupHandlerUtils
    {
        public static Func<Task> CreateHandler(Func<object> createInstance, MethodInfo method, bool isInit)
        {
            return CreateHandler(
                args => method.Invoke(method.IsStatic ? null : createInstance(), args),
                method.GetParameters(),
                method.ReturnType,
                isInit);
        }

        public static Func<Task> CreateHandler(Delegate impl, bool isInit)
        {
            return CreateHandler(
                args => impl.DynamicInvoke(args),
                impl.Method.GetParameters(),
                impl.Method.ReturnType,
                isInit);
        }

        public static Func<Task> CreateHandler(Func<object?[], object?> handler, ParameterInfo[] parameters, Type resultType, bool isInit)
        {
            return isInit ?
                async () => await ProcessAwaitable(handler(CastArguments(parameters, Array.Empty<object?>())), resultType).ConfigureAwait(false) :
                WrapHandler(async () =>
                {
                    var arguments = await AsyncLocals.FabricService.ReadExecutionParameters(AsyncLocals.Execution).ConfigureAwait(false);
                    var castArguments = CastArguments(parameters, arguments);
                    var result = handler(castArguments);
                    await ProcessResult(result, resultType).ConfigureAwait(false);
                });
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        public static Func<Task> WrapHandler(Func<Task> handler)
        {
            return async () =>
            {
                try
                {
                    await handler().ConfigureAwait(false);
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

        public static object?[] CastArguments(ParameterInfo[] parameters, object?[] arguments)
        {
            var castArguments = new object?[parameters.Length];
            for (var i = 0 ; i < parameters.Length ; i++)
            {
                try
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
                catch (Exception e)
                {
                    throw new ArgumentException($"Failed decoding parameter {i + 1} ({parameters[i]})", e);
                }
            }

            return castArguments;
        }

        public static object? CastArgument(object? arg, Type parameterType)
        {
            return arg != null && parameterType.IsAssignableFrom(arg.GetType())
                ? arg
                : arg is ArrayList arrayList && parameterType == typeof(object[])
                    ? arrayList.Cast<object>().ToArray()
                    : Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);
        }

        public static async Task WriteAsyncEnumerable<T>(IAsyncEnumerable<T> values)
        {
            await foreach (var value in values)
            {
                await AsyncLocals.FabricService.WriteStreamItem(AsyncLocals.Execution, value).ConfigureAwait(false);
            }
        }

        public static async Task WriteAsyncEnumerableWithKeys<T>(IAsyncEnumerable<(long, T)> values)
        {
            await foreach (var (key, value) in values)
            {
                await AsyncLocals.FabricService.WriteStreamItem(AsyncLocals.Execution, key, value).ConfigureAwait(false);
            }
        }

        public static async Task WriteResult(object? result, Type resultType)
        {
            if (result == null)
            {
                await AsyncLocals.FabricService.WriteExecutionFinished(AsyncLocals.Execution).ConfigureAwait(false);
            }
            else
            {
                object?[] results;

                if (result is ITuple tuple)
                {
                    results = new object?[tuple.Length];
                    for (var i = 0 ; i < results.Length ; i++)
                    {
                        results[i] = tuple[i];
                    }
                }
                else
                {
                    results = result is object?[] _results && resultType == typeof(object[]) ? _results : (new object?[] { result });
                }

                await AsyncLocals.FabricService.WriteExecutionResult(AsyncLocals.Execution, results).ConfigureAwait(false);
            }
        }

        public static async Task ProcessResult(object? result, Type resultType)
        {
            (result, resultType) = await ProcessAwaitable(result, resultType).ConfigureAwait(false);
            (result, resultType) = await ProcessStream(result, resultType).ConfigureAwait(false);
            await WriteResult(result, resultType).ConfigureAwait(false);
        }

        private static async ValueTask<(object?, Type)> ProcessAwaitable(object? result, Type resultType)
        {
            var isAwaitable = resultType.GetMethod(nameof(Task.GetAwaiter)) != null;
            if (isAwaitable)
            {
                if (resultType.IsGenericType)
                {
                    return (await (dynamic)result!, resultType.GetGenericArguments()[0]);
                }
                else
                {
                    await (dynamic)result!;
                    return (null, typeof(void));
                }
            }
            return (result, resultType);
        }

        private static readonly MethodInfo WriteAsyncEnumerableMethod = typeof(PerperStartupHandlerUtils).GetMethod(nameof(WriteAsyncEnumerable))!;

        private static readonly MethodInfo WriteAsyncEnumerableWithKeysMethod = typeof(PerperStartupHandlerUtils).GetMethod(nameof(WriteAsyncEnumerableWithKeys))!;

        private static async ValueTask<(object?, Type)> ProcessStream(object? result, Type resultType)
        {
            var asyncEnumerableInterface = GetGenericInterface(resultType, typeof(IAsyncEnumerable<>));
            if (asyncEnumerableInterface != null)
            {
                var itemType = asyncEnumerableInterface.GetGenericArguments()[0]!;

                MethodInfo processMethod;

                if (itemType.IsGenericType
                    && itemType.GetGenericTypeDefinition().Equals(typeof(ValueTuple<,>))
                    && itemType.GetGenericArguments()[0].Equals(typeof(long)))
                {
                    itemType = itemType.GetGenericArguments()[1];
                    processMethod = WriteAsyncEnumerableWithKeysMethod.MakeGenericMethod(itemType);
                }
                else
                {
                    processMethod = WriteAsyncEnumerableMethod.MakeGenericMethod(itemType);
                }

                await AsyncLocals.FabricService.WaitListenerAttached(AsyncLocals.Execution, AsyncLocals.CancellationToken).ConfigureAwait(false);

                await ((Task)processMethod.Invoke(null, new[] { result })!).ConfigureAwait(false);

                return (null, typeof(void));
            }

            return (result, resultType);
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
    }
}