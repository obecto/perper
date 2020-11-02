using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Perper.WebJobs.Extensions.Protocol.Notifications;

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
            var channel = _context.CreateChannel(NotificationType.StreamTrigger, _delegateName);
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification.Stream;
            }
        }

        public async IAsyncEnumerable<(string, T)> StreamParameterItemUpdates<T>(string streamName,
            string parameterName, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel(NotificationType.StreamParameterItemUpdate, _delegateName, streamName,
                parameterName, typeof(T));
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return (notification.ParameterStream, (T)notification.ParameterStreamItem);
            }
        }

        public async IAsyncEnumerable<(string, string)> WorkerTriggers(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel(NotificationType.WorkerTrigger, _delegateName);
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return (notification.Stream, notification.Worker);
            }
        }

        public async IAsyncEnumerable<string> WorkerResultSubmissions(string streamName, string workerName,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = _context.CreateChannel(NotificationType.WorkerResult, _delegateName, streamName, workerName);
            await foreach (var notification in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return notification.Stream;
            }
        }
    }
}