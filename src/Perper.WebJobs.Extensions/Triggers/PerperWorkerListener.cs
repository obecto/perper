using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperWorkerListener : IListener
    {
        private readonly PerperWorkerTriggerAttribute _attribute;
        private readonly string _delegateName;
        private readonly IConverter<PerperWorkerContext, object> _triggerValueConverter;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly IPerperFabricContext _context;

        private readonly CancellationTokenSource _listenCancellationTokenSource;

        private Task _listenTask;

        public PerperWorkerListener(PerperWorkerTriggerAttribute attribute, string delegateName, 
            IConverter<PerperWorkerContext, object> triggerValueConverter,
            ITriggeredFunctionExecutor executor, IPerperFabricContext context)
        {
            _attribute = attribute;
            _delegateName = delegateName;
            _triggerValueConverter = triggerValueConverter;
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
            var triggers = _context.GetNotifications(_delegateName).WorkerTriggers(cancellationToken);
            await foreach (var (streamName, workerName) in triggers.WithCancellation(cancellationToken))
            {
                var triggerValue = new PerperWorkerContext {StreamName = streamName, WorkerName = workerName};
                await _executor.TryExecuteAsync(
                    new TriggeredFunctionData {TriggerValue = _triggerValueConverter.Convert(triggerValue)},
                    cancellationToken);
            }
        }
    }
}