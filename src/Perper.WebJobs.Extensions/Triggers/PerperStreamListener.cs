using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamListener : IListener
    {
        private ITriggeredFunctionExecutor _executor;
        private PerperFabricContext _context;
        private string _funcName;

        private PipeReader _pipeReader;
        
        public PerperStreamListener(string funcName, PerperFabricContext context, ITriggeredFunctionExecutor executor)
        {
            _funcName = funcName;
            _context = context;
            _executor = executor;
        }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var readerResult = await _pipeReader.ReadAsync(cancellationToken);
            
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }

        public void Cancel()
        {
            throw new System.NotImplementedException();
        }
    }
}