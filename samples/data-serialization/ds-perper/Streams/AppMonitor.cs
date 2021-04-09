using System;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Triggers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ds_perper.Models;
using Microsoft.Extensions.Logging;

namespace ds_perper.Streams
{
    public static class AppMonitor
    {
        [FunctionName(nameof(AppMonitor))]
        public static async Task RunAsync(
            [PerperTrigger] dynamic parameters,
            CancellationToken cancellationToken,
            ILogger logger)
        {
            logger.LogInformation("AppMonitor on standby...");
            IAsyncEnumerable<dynamic> dataStream = parameters.dataStream;
            int received = 0;

            await foreach (var obj in dataStream.WithCancellation(cancellationToken))
            {
                if (obj == null)
                {
                    logger.LogInformation("Object is null");
                }
                logger.LogInformation($"{obj}, received: {received++}");
                await Task.Delay(10000);
            }
        }
    }
}