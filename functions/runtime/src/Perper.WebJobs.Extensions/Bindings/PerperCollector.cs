using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Microsoft.Azure.WebJobs;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperCollector<T> : IAsyncCollector<T>
    {
        private readonly ICacheClient<long, T> cache;

        public PerperCollector(IIgniteClient ignite, string stream)
        {
            cache = ignite.GetCache<long, T>(stream);
        }

        public Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            // NOTE: Should probably be made to use the same time format as fabric
            return cache.PutAsync(DateTime.UtcNow.Ticks, item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}