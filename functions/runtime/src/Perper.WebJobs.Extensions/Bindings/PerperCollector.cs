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
        private readonly ICacheClient<long, object?> cache;
        private readonly Func<object?, object?> converter;

        public PerperCollector(IIgniteClient ignite, PerperBinarySerializer serializer, string stream)
        {
            cache = ignite.GetCache<long, object?>(stream);
            converter = serializer.GetObjectConverters(typeof(T)).to;
        }

        public Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            // NOTE: Should probably be made to use the same time format as fabric
            return cache.PutAsync(DateTime.UtcNow.Ticks, converter.Invoke(item));
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}