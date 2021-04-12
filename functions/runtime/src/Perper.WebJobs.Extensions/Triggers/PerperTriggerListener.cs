using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Cache.Notifications;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerListener : IListener
    {
        private readonly FabricService _fabric;
        private readonly string _delegate;
        private readonly IIgniteClient _ignite;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly ILogger _logger;

        private readonly CancellationTokenSource _listenCancellationTokenSource;

        private Task? _listenTask;

        public PerperTriggerListener(FabricService fabric, string @delegate, IIgniteClient ignite,
            ITriggeredFunctionExecutor executor, ILogger logger)
        {
            _fabric = fabric;
            _delegate = @delegate;
            _ignite = ignite;
            _executor = executor;
            _logger = logger;

            _listenCancellationTokenSource = new CancellationTokenSource();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _listenTask = ListenAsync(_listenCancellationTokenSource.Token);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _listenCancellationTokenSource.Cancel();
            await Task.WhenAny(_listenTask!, Task.Delay(Timeout.Infinite, cancellationToken));
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
            var taskCollection = new TaskCollection();
            await foreach (var (key, notification) in _fabric.GetNotifications(_delegate).WithCancellation(cancellationToken))
            {
                taskCollection.Add(async () =>
                {
                    if (notification is StreamTriggerNotification streamNotification)
                    {
                        await _fabric.ConsumeNotification(key, cancellationToken); // Consume first, since execution might never end in this case
                        await ExecuteAsync(new PerperTriggerValue(false, streamNotification.Stream), cancellationToken);
                    }
                    else if (notification is CallTriggerNotification callNotification)
                    {
                        await ExecuteAsync(new PerperTriggerValue(true, callNotification.Call), cancellationToken);
                        await _fabric.ConsumeNotification(key, cancellationToken);
                    }
                });
            }
            await taskCollection.GetTask();
        }

        private async Task ExecuteAsync(PerperTriggerValue triggerValue, CancellationToken cancellationToken)
        {
            var input = new TriggeredFunctionData { TriggerValue = triggerValue };
            var result = await _executor.TryExecuteAsync(input, cancellationToken);

            string? error = null;

            if (result.Exception != null && !(result.Exception is OperationCanceledException))
            {
                _logger.LogError($"Exception during execution: {result.Exception}");
                error = (result.Exception.InnerException ?? result.Exception).Message;
            }

            if (triggerValue.IsCall)
            {
                // TODO: Can we somehow detect that PerperTriggerValueBinder was already invoked for this?
                var call = triggerValue.InstanceName;
                var callsCache = _ignite.GetCache<string, CallData>("calls");
                var callDataResult = await callsCache.TryGetAsync(call);
                if (callDataResult.Success)
                {
                    var callData = callDataResult.Value;
                    callData.Finished = true;
                    callData.Error = error;
                    await callsCache.ReplaceAsync(call, callData);
                }
            }
        }
    }
}