using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Perper.Protocol.Notifications;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricNotifications
    {
        private readonly string _streamName;
        private readonly PerperFabricContext _context;

        public PerperFabricNotifications(string streamName, PerperFabricContext context)
        {
            _streamName = streamName;
            _context = context;
        }

        public async IAsyncEnumerable<object> StreamTriggers(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<StreamTriggerNotification>(_streamName);
            await foreach (var _ in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return null;
            }
        }

        public async IAsyncEnumerable<(string, long)> StreamParameterItemUpdates<T>(string parameterName,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<StreamParameterItemUpdateNotification>(_streamName, typeof(T), parameterName);
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return (notification.ItemStreamName, notification.ItemKey);
            }
        }

        public async IAsyncEnumerable<object> WorkerTriggers(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<WorkerTriggerNotification>(_streamName);
            await foreach (var _ in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return null;
            }
        }

        public async IAsyncEnumerable<object> WorkerResultSubmissions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<WorkerResultSubmitNotification>(_streamName);
            await foreach (var _ in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return null;
            }
        }
    }
}