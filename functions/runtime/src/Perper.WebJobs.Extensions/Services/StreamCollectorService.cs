using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;

namespace Perper.WebJobs.Extensions.Services
{
    public class StreamCollectorService<T>
    {
        private readonly IIgniteClient _ignite;

        public StreamCollectorService(IIgniteClient ignite)
        {
            _ignite = ignite;
        }

        public Task AddAsync(T item, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Add(IAsyncEnumerable<T> items)
        {
            var task = items.ForEachAsync(async item =>
            {
                await Task.Delay(1);
            });
        }
    }
}