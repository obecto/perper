using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace AgentPong.Streams
{
    public class AgentPongStream
    {
        [FunctionName(nameof(AgentPongStream))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("input")] IAsyncEnumerable<string> input,
            [Perper("output")] IAsyncCollector<string> output,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            await foreach (var message in input.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"AgentPong received {message}");

                await PongAsync(output, cancellationToken);
            }
        }

        private static async Task PongAsync(IAsyncCollector<string> output, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            await output.AddAsync("pong", cancellationToken);
        }
    }
}