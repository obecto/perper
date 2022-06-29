using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Extensions;
using Perper.Model;

namespace Perper.Application.Handlers
{
    internal static class AsyncEnumerablePerperHandlerHelper
    {
        public static ParameterInfo PerperStreamParameter = typeof(AsyncEnumerablePerperHandlerHelper)
            .GetMethod(nameof(PerperStreamParameterMethod))!
            .GetParameters()[0];
#pragma warning disable CA1801, IDE0060
        public static void PerperStreamParameterMethod(PerperStream perperStream) { }
#pragma warning restore CA1801, IDE0060
    }

    public class AsyncEnumerablePerperHandler<TItem> : IPerperHandler<VoidStruct>
    {
        private readonly IServiceProvider Services;
        protected IPerper Perper { get; }
        protected IPerperHandler<IAsyncEnumerable<TItem>> Inner { get; }

        public AsyncEnumerablePerperHandler(IPerperHandler<IAsyncEnumerable<TItem>> inner, IServiceProvider services)
        {
            Inner = inner;
            Perper = services.GetRequiredService<IPerper>();
            Services = services;
        }

        // public ParameterInfo[]? GetParameters() => new[] { AsyncEnumerablePerperHandlerHelper.PerperStreamParameter }.Concat(Inner.GetParameters() ?? Enumerable.Empty<ParameterInfo>()).ToArray();
        public ParameterInfo[]? GetParameters() => Inner.GetParameters();

        public async Task<VoidStruct> Invoke(PerperExecutionData executionData, object?[] arguments)
        {
            // var stream = (PerperStream)arguments[0]!;
            // var items = await Inner.Invoke(executionData, arguments.Skip(1).ToArray()).ConfigureAwait(false);
            var stream = new PerperStream(executionData.Execution.Execution);

            await Perper.Streams.WaitForListenerAsync(stream, executionData.CancellationToken).ConfigureAwait(false);

            var items = await Inner.Invoke(executionData, arguments).ConfigureAwait(false);

            // HACK: ! Currently MethodPerperHandler ought to take care of scopes and AsyncLocals context. However, the Decorator pattern in AsyncEnumerablePerperHandler means that the scope from MethodPerperHandler disappears too early. Note that this may result in scoped services behaving super erratically when injected through a constructor.
            using var scope = Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<PerperScopeService>().SetExecution(executionData);
            using (scope.ServiceProvider.GetRequiredService<IPerperContext>().UseContext())
            {
                await WriteItemsAsync(stream, items, executionData.CancellationToken).ConfigureAwait(false);
            }

            return VoidStruct.Value;
        }

        protected virtual async Task WriteItemsAsync(PerperStream stream, IAsyncEnumerable<TItem> items, CancellationToken cancellationToken)
        {
            await foreach (var value in items.WithCancellation(cancellationToken))
            {
                await Perper.Streams.WriteItemAsync(stream, value).ConfigureAwait(false);
            }
        }
    }
}