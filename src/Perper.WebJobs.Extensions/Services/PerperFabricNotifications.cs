using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Perper.Protocol.Notifications;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricNotifications
    {
        private readonly string _delegateName;
        private readonly PerperFabricContext _context;

        public PerperFabricNotifications(string delegateName, PerperFabricContext context)
        {
            _delegateName = delegateName;
            _context = context;
        }

        public async IAsyncEnumerable<string> StreamTriggers(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<StreamTriggerNotification>(_delegateName);
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification.StreamName;
            }
        }

        public async IAsyncEnumerable<(string, long)> StreamParameterItemUpdates<T>(string streamName,
            string parameterName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<StreamParameterItemUpdateNotification>(_delegateName, streamName,
                parameterName, typeof(T));
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return (notification.ItemStreamName, notification.ItemKey);
            }
        }

        public async IAsyncEnumerable<(string, string)> WorkerTriggers(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<WorkerTriggerNotification>(_delegateName);
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return (notification.StreamName, notification.WorkerName);
            }
        }

        public async IAsyncEnumerable<string> WorkerResultSubmissions(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel<WorkerResultSubmitNotification>(_delegateName);
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification.StreamName;
            }
        }
    }
}