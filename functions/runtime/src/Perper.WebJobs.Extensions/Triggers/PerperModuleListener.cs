using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperModuleListener : IListener
    {
        private readonly PerperModuleTriggerAttribute _attribute;
        private readonly string _delegateName;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly IPerperFabricContext _context;
        private readonly ILogger _logger;
        private readonly PerperTriggerValueConverter<PerperModuleContext> _triggerValueConverter;

        private readonly CancellationTokenSource _listenCancellationTokenSource;

        private Task _listenTask;

        public PerperModuleListener(PerperModuleTriggerAttribute attribute, string delegateName,
            ITriggeredFunctionExecutor executor, IPerperFabricContext context,
            ILogger logger, PerperTriggerValueConverter<PerperModuleContext> triggerValueConverter)
        {
            _attribute = attribute;
            _delegateName = delegateName;
            _executor = executor;
            _context = context;
            _logger = logger;
            _triggerValueConverter = triggerValueConverter;

            _listenCancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _context.StartListen(_delegateName);
            _listenTask = ListenAsync(_listenCancellationTokenSource.Token);

            if (_attribute.RunOnStartup)
            {
                var data = _context.GetData($"$launcher.{_delegateName}");
                await data.StreamActionAsync($"$launcher.{_delegateName}", "", new { });
                await data.CallWorkerAsync($"$launcher.{_delegateName}-{Guid.NewGuid().ToString()}", _delegateName, "", new { });
            }
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
            var triggerValue = new PerperModuleContext(streamName, _delegateName, workerName, _context);
            await _executor.TryExecuteAsync(
                new TriggeredFunctionData { TriggerValue = _triggerValueConverter.Convert(triggerValue) },
                cancellationToken);
        }
    }
}