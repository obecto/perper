using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperWorkerAsyncCollector<T> : IAsyncCollector<T>
    {
        private readonly string _streamName;
        private readonly string _workerName;
        private readonly IPerperFabricContext _context;

        public PerperWorkerAsyncCollector(string streamName, string workerName, IPerperFabricContext context)
        {
            _streamName = streamName;
            _workerName = workerName;
            _context = context;
        }

        public async Task AddAsync(T item, CancellationToken cancellationToken = new CancellationToken())
        {
            var data = _context.GetData(_streamName);
            await data.SubmitWorkerResultAsync(_workerName, item);
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}