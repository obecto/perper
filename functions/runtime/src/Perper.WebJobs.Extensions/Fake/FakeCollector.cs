using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeCollector<T> : IAsyncCollector<T>
    {
        private readonly ChannelWriter<object?> _channelWriter;

        public FakeCollector(ChannelWriter<object?> writer) => _channelWriter = writer;

        public async Task AddAsync(T item, CancellationToken cancellationToken = default) => await _channelWriter.WriteAsync(FakeConfiguration.Serialize(item), cancellationToken).ConfigureAwait(false);

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void Complete(Exception? exception = null) => _channelWriter.Complete(exception);
    }
}