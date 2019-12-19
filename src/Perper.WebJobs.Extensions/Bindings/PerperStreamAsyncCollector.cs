using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly IPerperFabricContext _context;
        private readonly string _streamName;

        public PerperStreamAsyncCollector(IPerperFabricContext context, string streamName)
        {
            _context = context;
            _streamName = streamName;
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = new CancellationToken())
        {
            var data = _context.GetData(_streamName);
            await data.AddStreamItemAsync(item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}