using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Extensions;
using Perper.Model;

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

        protected override ParameterInfo[]? GetParameters() => Method.GetParameters();

        protected override async Task<(Type, object?)> Handle(IServiceProvider serviceProvider, object?[] arguments)
        {
            var instance = Method.IsStatic ? null : CreateInstance?.Invoke(serviceProvider);

            using (serviceProvider.GetRequiredService<IPerperContext>().UseContext())
            {
                var result = Method.Invoke(instance, arguments);

                return await ProcessResult(serviceProvider, Method.ReturnType, result).ConfigureAwait(false);
            }
        }

        // TODO: Convert to decorator pattern (yayy genericss)
        public static async Task<(Type, object?)> ProcessResult(IServiceProvider serviceProvider, Type resultType, object? result)
        {
            (resultType, result) = await ProcessAwaitable(serviceProvider, resultType, result).ConfigureAwait(false);
            (resultType, result) = await ProcessStream(serviceProvider, resultType, result).ConfigureAwait(false);
            return (resultType, result);
        }

        private static readonly MethodInfo WriteAsyncEnumerableMethod = typeof(MethodPerperHandler).GetMethod(nameof(WriteAsyncEnumerable))!;
        public static async Task WriteAsyncEnumerable<T>(IPerperStreams streams, PerperStream stream, IAsyncEnumerable<T> values)
        {
            await foreach (var value in values)
            {
                await streams.WriteItemAsync(stream, value).ConfigureAwait(false);
            }
        }

        private static readonly MethodInfo WriteAsyncEnumerableWithKeysMethod = typeof(MethodPerperHandler).GetMethod(nameof(WriteAsyncEnumerableWithKeys))!;
        public static async Task WriteAsyncEnumerableWithKeys<T>(IPerperStreams streams, PerperStream stream, IAsyncEnumerable<(long, T)> values)
        {
            await foreach (var (key, value) in values)
            {
                await streams.WriteItemAsync(stream, key, value).ConfigureAwait(false);
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
                var streams = serviceProvider.GetRequiredService<IPerperContext>().Streams;
                var executionData = serviceProvider.GetRequiredService<PerperExecutionData>();
                var stream = new PerperStream(executionData.Execution.Execution);
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

                await streams.WaitForListenerAsync(stream, executionData.CancellationToken).ConfigureAwait(false);

                await ((Task)processMethod.Invoke(null, new[] { streams, stream, result })!).ConfigureAwait(false);

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