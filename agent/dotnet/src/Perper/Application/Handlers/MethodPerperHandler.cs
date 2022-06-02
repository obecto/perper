using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Extensions;
using Perper.Protocol;

namespace Perper.Application
{
    public class MethodPerperHandler : BasePerperHandler
    {
        public MethodInfo Method { get; }

        public Func<IServiceProvider, object?>? CreateInstance { get; }

        public MethodPerperHandler(string agent, string @delegate, Func<IServiceProvider, object?>? createInstance, MethodInfo method)
            : base(agent, @delegate)
        {
            Method = method;
            CreateInstance = createInstance;
        }

        public MethodPerperHandler(string agent, string @delegate, Type type, MethodInfo method)
            : this(agent, @delegate, (serviceProvider) => ActivatorUtilities.CreateInstance(serviceProvider, type), method)
        {
        }

        public MethodPerperHandler(string agent, string @delegate, Delegate handler)
            : this(agent, @delegate, (_) => handler.Target!, handler.Method)
        {
        }

        protected override async Task<object?[]> Handle(IServiceProvider serviceProvider, object?[] arguments)
        {
            var instance = Method.IsStatic ? null : CreateInstance?.Invoke(serviceProvider);
            var castArguments = CastArguments(Method.GetParameters(), arguments);

            AsyncLocals.SetConnection(serviceProvider.GetRequiredService<FabricService>());
            AsyncLocals.SetExecution(serviceProvider.GetRequiredService<FabricExecution>());

            var result = Method.Invoke(instance, castArguments);

            return await ProcessResult(serviceProvider, Method.ReturnType, result).ConfigureAwait(false);
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
                                paramsArray.SetValue(CastArgument(paramsType, arguments[i + j]), j);
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
                        castArgument = CastArgument(parameters[i].ParameterType, arguments[i]);
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

        public static object? CastArgument(Type parameterType, object? arg)
        {
            return arg != null && parameterType.IsAssignableFrom(arg.GetType())
                ? arg
                : arg is ArrayList arrayList && parameterType == typeof(object[])
                    ? arrayList.Cast<object>().ToArray()
                    : Convert.ChangeType(arg, parameterType, CultureInfo.InvariantCulture);
        }

        public static object?[] CastResult(Type resultType, object? result)
        {
            if (result == null)
            {
                return Array.Empty<object?>();
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

                return results;
            }
        }

        public static async Task<object?[]> ProcessResult(IServiceProvider serviceProvider, Type resultType, object? result)
        {
            (resultType, result) = await ProcessAwaitable(serviceProvider, resultType, result).ConfigureAwait(false);
            (resultType, result) = await ProcessStream(serviceProvider, resultType, result).ConfigureAwait(false);
            return CastResult(resultType, result);
        }

        private static readonly MethodInfo WriteAsyncEnumerableMethod = typeof(MethodPerperHandler).GetMethod(nameof(WriteAsyncEnumerable))!;
        public static async Task WriteAsyncEnumerable<T>(FabricService fabricService, FabricExecution fabricExecution, IAsyncEnumerable<T> values)
        {
            await foreach (var value in values)
            {
                await fabricService.WriteStreamItem(fabricExecution.Execution, value).ConfigureAwait(false);
            }
        }

        private static readonly MethodInfo WriteAsyncEnumerableWithKeysMethod = typeof(MethodPerperHandler).GetMethod(nameof(WriteAsyncEnumerableWithKeys))!;
        public static async Task WriteAsyncEnumerableWithKeys<T>(FabricService fabricService, FabricExecution fabricExecution, IAsyncEnumerable<(long, T)> values)
        {
            await foreach (var (key, value) in values)
            {
                await fabricService.WriteStreamItem(fabricExecution.Execution, key, value).ConfigureAwait(false);
            }
        }

        private static async ValueTask<(Type, object?)> ProcessAwaitable(IServiceProvider serviceProvider, Type resultType, object? result)
        {
            var isAwaitable = resultType.GetMethod(nameof(Task.GetAwaiter)) != null;
            if (isAwaitable)
            {
                if (resultType.IsGenericType)
                {
                    return (resultType.GetGenericArguments()[0], await (dynamic)result!);
                }
                else
                {
                    await (dynamic)result!;
                    return (typeof(void), null);
                }
            }
            return (resultType, result);
        }

        private static async ValueTask<(Type, object?)> ProcessStream(IServiceProvider serviceProvider, Type resultType, object? result)
        {
            var asyncEnumerableInterface = GetGenericInterface(resultType, typeof(IAsyncEnumerable<>));
            if (asyncEnumerableInterface != null)
            {
                var fabricService = serviceProvider.GetRequiredService<FabricService>();
                var fabricExecution = serviceProvider.GetRequiredService<FabricExecution>();
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

                await fabricService.WaitListenerAttached(fabricExecution.Execution, fabricExecution.CancellationToken).ConfigureAwait(false);

                await ((Task)processMethod.Invoke(null, new[] { fabricService, fabricExecution, result })!).ConfigureAwait(false);

                return (typeof(void), null);
            }

            return (resultType, result);
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