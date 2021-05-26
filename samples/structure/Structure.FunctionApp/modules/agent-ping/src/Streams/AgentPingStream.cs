using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace AgentPing.Streams
{
    public class AgentPingStream
    {
        [FunctionName(nameof(AgentPingStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("input")] IAsyncEnumerable<string> input,
            [Perper("output")] IAsyncCollector<string> output,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            await PingAsync(output, cancellationToken);

            await foreach (var message in input.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"AgentPing received {message}");
                await PingAsync(output, cancellationToken);
            }
        }

        private static async Task PingAsync(IAsyncCollector<string> output, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            await output.AddAsync("ping", cancellationToken);
        }
    }
}