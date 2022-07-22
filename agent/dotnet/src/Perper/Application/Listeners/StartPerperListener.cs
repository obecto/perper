using System;

using Perper.Application.Handlers;
using Perper.Model;

namespace Perper.Application.Listeners
{
    public class StartPerperListener : ExecutionPerperListener
    {
        public StartPerperListener(string agent, IPerperHandler handler, IServiceProvider services) : base(agent, PerperAgentsExtensions.StartFunctionName, handler, services)
        {
        }
    }
}

/*
namespace Perper.Application.Listeners
{
    public class StartPerperListener : IPerperListener
    {
        public const string FallbackStartDelegate = "Startup";

        private readonly IPerperListener listener;
        private readonly IPerperListener fallbackListener;

        public StreamPerperListener(string agent, IPerperHandler handler, IServiceProvider services)
        {
            listener = new ExecutionPerperListener(agent, PerperAgentsExtensions.StartFunctionName, handler, serviceProvider);
            fallbackListener = new ExecutionPerperListener(agent, FallbackStartDelegate, handler, serviceProvider);
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

        public override string ToString() => $"{GetType()}({listener}, {fallbackListener})";
    }
}*/