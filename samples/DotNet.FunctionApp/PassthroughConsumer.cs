using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class PassthroughConsumer
    {
        [FunctionName("PassthroughConsumer")]
        public static async Task RunAsync([PerperStreamTrigger] PerperStreamContext context,
            [Perper("processor")] IPerperStream processor,
            ILogger logger, CancellationToken cancellationToken)
        {
            logger.LogInformation($"Starting pass-through consumer");
            await using var consumer =
                await context.StreamActionAsync("NamedConsumer", typeof(Consumer), new {processor = processor.Subscribe()});

            await context.BindOutput(cancellationToken);
        }
    }
}