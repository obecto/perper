using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.Protocol.Cache;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly string _streamName;
        private readonly IPerperFabricContext _context;
        
        public PerperStreamAsyncCollector(string streamName, IPerperFabricContext context)
        {
            _streamName = streamName;
            _context = context;
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = new CancellationToken())
        {
            var data = _context.GetData(_streamName);

            if (typeof(T) == typeof(IPerperStream))
            {
                await data.AddStreamItemAsync((item as PerperFabricStream)!.StreamRef);
            }
            else
            {
                await data.AddStreamItemAsync(item);
            }
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}