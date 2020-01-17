using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNet.FunctionApp.Model;
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
            [PerperStream("processor")] IAsyncEnumerable<Data<int, string>> processor,
            ILogger logger, CancellationToken cancellationToken)
        {
            await foreach (var data in processor.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"Consumer stream receives: {data.Value}");
            }
        }
    }
}