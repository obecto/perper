using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Extensions;
using Perper.Model;

namespace Perper.Application.Handlers
{
    public class MethodPerperHandler : IPerperHandler
    {
        private readonly Func<IServiceProvider, object?>? CreateInstance;
        private readonly MethodInfo Method;
        private readonly IServiceProvider Services;
        private readonly IPerper Perper;
        private readonly HandlerFunc Handler;

        public bool IsStream { get; protected set; }

        public MethodPerperHandler(Func<IServiceProvider, object?>? createInstance, MethodInfo method, IServiceProvider services)
        {
            CreateInstance = method.IsStatic ? null : createInstance;
            Method = method;
            Services = services;
            Perper = services.GetRequiredService<IPerper>();

            Handler = WrapHandler((context, arguments) =>
            {
                var instance = CreateInstance?.Invoke(context.ScopedServices);
                return ValueTask.FromResult(Method.Invoke(instance, arguments));
            }, Method.ReturnType);
        }

        public MethodPerperHandler(Type type, MethodInfo method, IServiceProvider services)
            : this((serviceProvider) => ActivatorUtilities.CreateInstance(serviceProvider, type), method, services) { }

        public MethodPerperHandler(Delegate handler, IServiceProvider services)
            : this((_) => handler.Target!, handler.Method, services) { }

        private static readonly ParameterInfo PerperStreamParameter = typeof(MethodPerperHandler)
            .GetMethod(nameof(PerperStreamParameterMethod), BindingFlags.NonPublic | BindingFlags.Static)!
            .GetParameters()[0];
#pragma warning disable CA1801, IDE0060
        private static void PerperStreamParameterMethod(PerperStream perperStream) { }
#pragma warning restore CA1801, IDE0060

        public ParameterInfo[]? GetParameters() => IsStream ?
            new[] { PerperStreamParameter }.Concat(Method.GetParameters()).ToArray() :
            Method.GetParameters();

        public async Task Invoke(PerperExecutionData executionData, object?[] arguments)
        {
#pragma warning disable CA2007
            await using var scope = Services.CreateAsyncScope();
#pragma warning restore CA2007

            scope.ServiceProvider.GetRequiredService<PerperScopeService>().SetExecutionData(executionData);

            using (scope.ServiceProvider.GetRequiredService<IPerperContext>().UseContext())
            {
                await Handler(new HandlerContext(scope.ServiceProvider, executionData), arguments).ConfigureAwait(false);
            }
        }

        protected record HandlerContext(IServiceProvider ScopedServices, PerperExecutionData ExecutionData);

        protected delegate ValueTask<object?> HandlerFunc(HandlerContext context, object?[] arguments);

        protected HandlerFunc WrapHandler(HandlerFunc handler, Type resultType)
        {
            (handler, resultType) = HandleAwaitingTasks(handler, resultType);
            (handler, resultType) = HandleWritingStream(handler, resultType);
            (handler, resultType) = HandleWritingResult(handler, resultType);
            System.Diagnostics.Debug.Assert(resultType == typeof(void));
            return handler;
        }

        protected static (HandlerFunc, Type) HandleAwaitingTasks(HandlerFunc next, Type resultType)
        {
            if (resultType == typeof(Task))
            {
                return (async (HandlerContext context, object?[] arguments) =>
                {
                    var result = await next(context, arguments).ConfigureAwait(false);
                    if (result != null)
                    {
                        await ((Task)result).ConfigureAwait(false);
                    }
                    return null;
                }, typeof(void));
            }
            else if (resultType == typeof(ValueTask))
            {
                return (async (HandlerContext context, object?[] arguments) =>
                {
                    var result = await next(context, arguments).ConfigureAwait(false);
                    if (result != null)
                    {
                        await ((ValueTask)result).ConfigureAwait(false);
                    }
                    return null;
                }, typeof(void));
            }
            else if (resultType.IsGenericType && (resultType.GetGenericTypeDefinition() == typeof(Task<>) || resultType.GetGenericTypeDefinition() == typeof(ValueTask<>)))
            {
                return (async (HandlerContext context, object?[] arguments) =>
                {
                    var result = await next(context, arguments).ConfigureAwait(false);
                    return result is null ? null : await (dynamic)result;
                }, resultType.GenericTypeArguments[0]);
            }
            else
            {
                return (next, resultType);
            }
        }

        private static readonly MethodInfo WriteItemsMethod = typeof(PerperStreamsExtensions).GetMethod(nameof(PerperStreamsExtensions.WriteItemsAsync))!;
        private static readonly MethodInfo WriteKeyedItemsMethod = typeof(PerperStreamsExtensions).GetMethod(nameof(PerperStreamsExtensions.WriteKeyedItemsAsync))!;

        protected (HandlerFunc, Type) HandleWritingStream(HandlerFunc next, Type resultType)
        {
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                IsStream = true;

                var itemType = resultType.GenericTypeArguments[0];

                var writeItemsMethod =
                    (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(ValueTuple<,>) && itemType.GenericTypeArguments[0] == typeof(long))
                        ? WriteKeyedItemsMethod.MakeGenericMethod(itemType.GenericTypeArguments[1])
                        : WriteItemsMethod.MakeGenericMethod(itemType);

                return (async (HandlerContext context, object?[] arguments) =>
                {
                    if (context.ExecutionData.IsSynthetic)
                    {
                        return await next(context, arguments).ConfigureAwait(false);
                    }

                    var stream = (PerperStream)arguments[0]!;
                    arguments = arguments.Skip(1).ToArray();

                    var cancellationToken = context.ExecutionData.CancellationToken;

                    await Perper.Streams.WaitForListenerAsync(stream, cancellationToken).ConfigureAwait(false);

                    var result = await next(context, arguments).ConfigureAwait(false);
                    if (result is null)
                    {
                        return null;
                    }

                    await ((Task)writeItemsMethod.Invoke(null, new object?[] { Perper.Streams, stream, result, cancellationToken })!).ConfigureAwait(false);

                    return null;
                }, typeof(void));
            }
            else
            {
                return (next, resultType);
            }
        }

        private static readonly MethodInfo WriteResultMethod = typeof(IPerperExecutions).GetMethods().Single(x => x.Name == nameof(IPerperExecutions.WriteResultAsync) && x.IsGenericMethod);

        protected (HandlerFunc, Type) HandleWritingResult(HandlerFunc next, Type resultType)
        {
            var writeResultMethod = resultType == typeof(void) ? null : WriteResultMethod.MakeGenericMethod(resultType);
            return (async (HandlerContext context, object?[] arguments) =>
            {
                var result = await next(context, arguments).ConfigureAwait(false);

                if (!context.ExecutionData.IsSynthetic)
                {
                    if (writeResultMethod != null)
                    {
                        await ((Task)writeResultMethod.Invoke(Perper.Executions, new object?[] { context.ExecutionData.Execution, result })!).ConfigureAwait(false);
                    }
                    else
                    {
                        await Perper.Executions.WriteResultAsync(context.ExecutionData.Execution).ConfigureAwait(false);
                    }
                }

                return null;
            }, typeof(void));
        }
    }
}