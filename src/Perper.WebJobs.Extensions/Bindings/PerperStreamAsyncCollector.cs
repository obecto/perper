using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamAsyncCollector<T> : IAsyncCollector<T>
    {
        private PerperFabricOutput _output;
        private IBinary _binary;

        public PerperStreamAsyncCollector(PerperFabricOutput output, IBinary binary)
        {
            _output = output;
            _binary = binary;
        }
        
        public async Task AddAsync(T item, CancellationToken cancellationToken = new CancellationToken())
        {
            await _output.AddAsync(_binary.ToBinary<IBinaryObject>(item));
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}