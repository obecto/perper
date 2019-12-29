using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamListener : IListener
    {
        private readonly PerperStreamTriggerAttribute _attribute;
        private readonly string _delegateName;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly IPerperFabricContext _context;

        private readonly CancellationTokenSource _listenCancellationTokenSource;

        private Task _listenTask;

        public PerperStreamListener(PerperStreamTriggerAttribute attribute, string delegateName,
            ITriggeredFunctionExecutor executor, IPerperFabricContext context)
        {
            _attribute = attribute;
            _delegateName = delegateName;
            _executor = executor;
            _context = context;

            _listenCancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _context.StartListen(_delegateName);
            
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
            if (_attribute.RunOnStartup)
            {
                await ExecuteAsync(string.Empty, cancellationToken);
            }
            else
            {
                var triggers = _context.GetNotifications(_delegateName).StreamTriggers(cancellationToken);
                await foreach (var streamName in triggers.WithCancellation(cancellationToken))
                {
                    await ExecuteAsync(streamName, cancellationToken);
                }
            }
        }

        private async Task ExecuteAsync(string streamName, CancellationToken cancellationToken)
        {
            var triggerValue = new PerperStreamContext(streamName, _delegateName, _context);
            await _executor.TryExecuteAsync(new TriggeredFunctionData {TriggerValue = triggerValue}, cancellationToken);
        }
    }
}