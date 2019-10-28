using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class StreamListener : IListener
    {
        private ITriggeredFunctionExecutor _executor;

        public StreamListener(ITriggeredFunctionExecutor executor)
        {
            _executor = executor;
        }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
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