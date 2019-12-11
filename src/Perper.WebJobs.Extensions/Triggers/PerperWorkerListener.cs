using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperWorkerListener : IListener
    {
        private readonly string _streamName;
        private readonly string _parameterName;
        private readonly PerperFabricContext _context;
        private readonly IBinary _binary;
        private readonly ITriggeredFunctionExecutor _executor;
        
        public PerperWorkerListener(string streamName, string parameterName, PerperFabricContext context, IBinary binary, ITriggeredFunctionExecutor executor)
        {
            _streamName = streamName;
            _parameterName = parameterName;
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
            var workerObject = await input.GetWorkerObjectAsync(default);
            await _executor.TryExecuteAsync(
                new TriggeredFunctionData {TriggerValue = workerObject.GetField<object>(_parameterName)},
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