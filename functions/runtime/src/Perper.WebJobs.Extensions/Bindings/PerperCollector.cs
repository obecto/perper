using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperCollector<T> : IAsyncCollector<T>
    {
        public Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}