using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Notification = Perper.WebJobs.Extensions.Cache.Notifications.Notification;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerListener : IListener
    {
        private readonly IAsyncEnumerable<Notification> _notifications;
        private readonly ITriggeredFunctionExecutor _executor;
        private readonly ILogger _logger;

        private readonly CancellationTokenSource _listenCancellationTokenSource;

        private Task? _listenTask;

        public PerperTriggerListener(IAsyncEnumerable<Notification> notifications,
            ITriggeredFunctionExecutor executor, ILogger logger)
        {
            _notifications = notifications;
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
            await Task.WhenAll(await _notifications.Select(
                notification => ExecuteAsync(notification, cancellationToken)).ToListAsync(cancellationToken));
        }

        private async Task ExecuteAsync(Notification notification, CancellationToken cancellationToken)
        {
            var input = new TriggeredFunctionData {TriggerValue = JObject.FromObject(notification)};
            var result = await _executor.TryExecuteAsync(input, cancellationToken);
            if (result.Exception != null && !(result.Exception is OperationCanceledException))
            {
                _logger.LogError($"Exception during execution': {result.Exception}");
            }
        }
    }
}