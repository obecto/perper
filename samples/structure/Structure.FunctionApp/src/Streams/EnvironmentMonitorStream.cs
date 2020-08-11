using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Structure.Streams
{
    public class EnvironmentMonitorStream
    {
        [FunctionName(nameof(EnvironmentMonitorStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("agentPingOutput")] IAsyncEnumerable<string> agentPingOutput,
            [Perper("agentPongOutput")] IAsyncEnumerable<string> agentPongOutput,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            await Task.WhenAll(
                agentPingOutput.ForEachAsync(message => logger.LogInformation($"Message in the environment: {message}"),
                    cancellationToken),
                agentPongOutput.ForEachAsync(message => logger.LogInformation($"Message in the environment: {message}"),
                    cancellationToken));
        }
    }
}