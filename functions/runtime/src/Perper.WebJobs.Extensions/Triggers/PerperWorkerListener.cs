using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;

        private readonly CancellationTokenSource _listenCancellationTokenSource;

        private Task _listenTask;

        public PerperWorkerListener(PerperWorkerTriggerAttribute attribute, string delegateName,
            ITriggeredFunctionExecutor executor, IPerperFabricContext context,
            ILogger logger, IConverter<PerperWorkerContext, object> triggerValueConverter)
        {
            _attribute = attribute;
            _delegateName = delegateName;
            _executor = executor;
            _context = context;
            _logger = logger;
            _triggerValueConverter = triggerValueConverter;

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
            var executions = new List<Task>();
            var triggers = _context.GetNotifications(_delegateName).WorkerTriggers(cancellationToken);
            await foreach (var (streamName, workerName) in triggers.WithCancellation(cancellationToken))
            {
                executions.Add(ExecuteAsync(streamName, workerName, cancellationToken));
            }
            await Task.WhenAll(executions);
        }

        private async Task ExecuteAsync(string streamName, string workerName, CancellationToken cancellationToken)
        {
            var triggerValue = new PerperWorkerContext { StreamName = streamName, WorkerName = workerName };
            await _executor.TryExecuteAsync(
                new TriggeredFunctionData { TriggerValue = _triggerValueConverter.Convert(triggerValue) },
                cancellationToken);
        }
    }
}