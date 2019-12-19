using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperWorkerListener : IListener
    {
        private readonly IPerperFabricContext _context;
        private readonly PerperWorkerAttribute _attribute;
        private readonly string _name;
        private readonly ITriggeredFunctionExecutor _executor;

        private readonly CancellationTokenSource _listenCancellationTokenSource;

        private Task _listenTask;

        public PerperWorkerListener(IPerperFabricContext context, PerperWorkerAttribute attribute, string name,
            ITriggeredFunctionExecutor executor)
        {
            _context = context;
            _attribute = attribute;
            _name = name;
            _executor = executor;

            _listenCancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _context.StartListen(_attribute.Stream);
            
            _listenTask = ListenAsync(_listenCancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _listenCancellationTokenSource.Cancel();
            await _listenTask.WithCancellation(cancellationToken);
        }

        public void Cancel()
        {
            StopAsync(CancellationToken.None).Wait();
        }

        public void Dispose()
        {
            _listenCancellationTokenSource.Dispose();
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            var triggers = _context.GetNotifications(_name).WorkerTriggers();
            await foreach (var _ in triggers.WithCancellation(cancellationToken))
            {
                var triggerValue = new PerperWorkerContext {StreamName = _attribute.Stream};
                await _executor.TryExecuteAsync(new TriggeredFunctionData {TriggerValue = triggerValue},
                    cancellationToken);
            }
        }
    }
}