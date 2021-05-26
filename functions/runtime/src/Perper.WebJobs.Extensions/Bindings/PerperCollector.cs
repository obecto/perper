using System;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Microsoft.Azure.WebJobs;

using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperCollector<T> : IAsyncCollector<T>
    {
        private readonly ICacheClient<long, object> cache;
        private readonly PerperBinarySerializer _serializer;

        public PerperCollector(IIgniteClient ignite, PerperBinarySerializer serializer, string stream)
        {
            cache = ignite.GetCache<long, object>(stream).WithKeepBinary<long, object>();
            _serializer = serializer;
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            var key = DateTime.UtcNow.Ticks - 62135596800000;

            var result = await cache.PutIfAbsentAsync(key, _serializer.SerializeRoot(item));
            if (!result)
            {
                throw new ArgumentException($"Duplicate stream item key! {key}");
            }
        }

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}