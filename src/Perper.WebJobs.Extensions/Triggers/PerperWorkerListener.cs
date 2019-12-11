using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperWorkerListener : IListener
    {
        private readonly string _streamName;
        private readonly PerperFabricContext _context;
        private readonly IBinary _binary;
        private readonly ITriggeredFunctionExecutor _executor;
        
        public PerperWorkerListener(string streamName, PerperFabricContext context, IBinary binary, ITriggeredFunctionExecutor executor)
        {
            _streamName = streamName;
            _context = context;
            _binary = binary;
            _executor = executor;
        }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var input = await _context.GetInput(_streamName);
            await input.GetWorkerObject(default);
            await _executor.TryExecuteAsync(
                new TriggeredFunctionData {TriggerValue = new PerperWorkerContext()},
                CancellationToken.None);
            //TODO: Handle function execution completion
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