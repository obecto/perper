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

        public Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            // NOTE: Should probably be made to use the same time format as fabric
            return cache.PutAsync(DateTime.UtcNow.Ticks, _serializer.SerializeRoot(item));
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}