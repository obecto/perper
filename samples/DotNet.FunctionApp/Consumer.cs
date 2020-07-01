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
        public static async Task RunAsync([PerperStreamTrigger] PerperStreamContext context,
            [Perper("processor")] IAsyncEnumerable<object> processor,
            ILogger logger, CancellationToken cancellationToken)
        {
            await foreach (var data in processor.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"Consumer stream receives: {data}");
            }
        }
    }
}