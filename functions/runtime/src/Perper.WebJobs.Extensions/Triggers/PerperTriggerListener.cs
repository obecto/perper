using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
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
                taskCollection.Add(async () => {
                    await ExecuteAsync(notification, cancellationToken);
                    await _fabric.ConsumeNotification(key);
                });
            }
            await taskCollection.GetTask();
        }

        private async Task ExecuteAsync(Notification notification, CancellationToken cancellationToken)
        {
            var trigger = JObject.FromObject(notification);
            var input = new TriggeredFunctionData {TriggerValue = trigger};
            var result = await _executor.TryExecuteAsync(input, cancellationToken);

            if (result.Exception != null && !(result.Exception is OperationCanceledException))
            {
                var call = (string) trigger["Call"]!;
                var callsCache = _ignite.GetCache<string, CallData>("calls");
                var callData = await callsCache.GetAsync(call);
                callData.Exception = result.Exception;
                callData.Finished = true;
                await callsCache.ReplaceAsync(call, callData);
                // _logger.LogError($"Exception during execution: {result.Exception}");
            }
        }
    }
}