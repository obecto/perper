using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CA1716

namespace Perper.Application.Listeners
{
    public abstract class CompositePerperListener : IPerperListener
    {
        public abstract string Agent { get; }

        private IPerperListener[]? Listeners;

        protected abstract IPerperListener[] GetListeners();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Listeners = GetListeners();
            foreach (var listener in Listeners)
            {
                await listener.StartAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var listener in Listeners!)
            {
                await listener.StartAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public override string ToString() => $"{GetType()}()";
    }
}