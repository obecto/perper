using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Consumer
    {
        [FunctionName("Consumer")]
        public static async Task RunAsync([PerperStreamTrigger("Consumer")] IPerperStreamContext context,
            [Perper("processor")] IAsyncEnumerable<int> processor,
            ILogger logger, CancellationToken cancellationToken)
        {
            await foreach (var value in processor.WithCancellation(cancellationToken))
            {
                logger.LogTrace($"Consumer stream receives: {value}");
            }
        }
    }
}