namespace SimpleAgent.Streams
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Perper.WebJobs.Extensions.Triggers;

    public static class Consumer
    {
        [FunctionName(nameof(Consumer))]
        public static async Task RunAsync(
            [PerperTrigger] IAsyncEnumerable<string[]> input,
            ILogger logger,
            CancellationToken cancellationToken)
        {
            await foreach (var messagesBatch in input.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"Received batch of {messagesBatch.Length} messages.\n{string.Join(", ", messagesBatch)}");
            }
        }
    }
}
