using System;
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
    public static class CyclicGenerator
    {
        [FunctionName("CyclicGenerator")]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [PerperStream("processor")] IAsyncEnumerable<Data<int, string>> processor,
            [PerperStream("output")] IAsyncCollector<Data<int, string>> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            await foreach (var data in processor.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"CyclicGenerator stream receives: {data.Value}");
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                await output.AddAsync(new Data<int, string> {Value = data.Value / 10}, cancellationToken);
            }
        }
    }
}