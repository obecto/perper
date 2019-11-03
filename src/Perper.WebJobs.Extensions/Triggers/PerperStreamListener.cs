using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamListener : IListener
    {
        private readonly string _cacheName;
        private readonly PerperFabricContext _context;
        private readonly IBinary _binary;
        private readonly ITriggeredFunctionExecutor _executor;
        
        public PerperStreamListener(string cacheName, PerperFabricContext context, IBinary binary, ITriggeredFunctionExecutor executor)
        {
            _cacheName = cacheName;
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
            var input = await _context.GetInput(_cacheName);
            await input.Listen(
                async o =>
                {
                    await _executor.TryExecuteAsync(
                        new TriggeredFunctionData
                            {TriggerValue = new PerperStreamContext(_context.GetOutput(_cacheName), _binary)},
                        CancellationToken.None);
                    //TODO: Handle function execution completion
                }, cancellationToken);
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