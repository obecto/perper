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

        public StreamPerperListener(string agent, string @delegate, PerperStreamOptions streamOptions, IPerperHandler handler, IServiceProvider serviceProvider)
        {
            var externalDelegate = @delegate;
            var internalDelegate = $"{@delegate}-stream";

            outerListener = new ExecutionPerperListener(agent, externalDelegate, new StartStreamPerperHandler(streamOptions, internalDelegate, serviceProvider), serviceProvider);
            innerListener = new ExecutionPerperListener(agent, internalDelegate, handler, serviceProvider);
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
    }
}