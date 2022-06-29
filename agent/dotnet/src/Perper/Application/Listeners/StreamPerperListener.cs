using System;
using System.Threading;
using System.Threading.Tasks;

using Perper.Application.Handlers;
using Perper.Model;

#pragma warning disable CA1716

namespace Perper.Application.Listeners
{
    public class StreamPerperListener : IPerperListener
    {
        private readonly IPerperListener outerListener;
        private readonly IPerperListener innerListener;

        public StreamPerperListener(string agent, string @delegate, PerperStreamOptions streamOptions, IPerperHandler<VoidStruct> handler, IServiceProvider serviceProvider)
        {
            var internalDelegate = $"{@delegate}-stream";

            outerListener = new ExecutionPerperListener<PerperStream>(agent, @delegate, new StreamPerperHandler(streamOptions, internalDelegate, serviceProvider), serviceProvider);
            innerListener = new ExecutionPerperListener<VoidStruct>(agent, internalDelegate, handler, serviceProvider);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await outerListener.StartAsync(cancellationToken).ConfigureAwait(false);
            await innerListener.StartAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await outerListener.StopAsync(cancellationToken).ConfigureAwait(false);
            await innerListener.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        public override string ToString() => $"{GetType()}({outerListener}, {innerListener})";

        public static IPerperListener From(string agent, string @delegate, PerperStreamOptions streamOptions, IPerperHandler handler, IServiceProvider services)
        {
            if (handler is IPerperHandler<VoidStruct> voidHandler)
            {
                return new StreamPerperListener(agent, @delegate, streamOptions, voidHandler, services);
            }
            else
            {
                throw new ArgumentOutOfRangeException($"Stream handler ({handler}) may not return a value. (Consider using PerperHandler.TryWrapAsyncEnumerable)");
            }
        }
    }
}