using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace AgentPong.Streams
{
    public class AgentPongRuntimeStream
    {
        [FunctionName(nameof(AgentPongRuntimeStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("input")] IAsyncEnumerable<IPerperStream> input,
            [Perper("output")] IAsyncCollector<IPerperStream> output,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            var agentInput = await input.FirstAsync(cancellationToken);
            var agentStream = await context.StreamFunctionAsync(typeof(AgentPongStream), new {input = agentInput.Subscribe()});
            await context.BindOutput(agentStream, cancellationToken);
        }
    }
}