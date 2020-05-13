using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.FunctionApp.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Generator
    {
        [FunctionName("Generator")]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("count")] int count,
            [Perper("tag")] string tag,
            [PerperStream("output")] IAsyncCollector<Data<int, string>> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            var lastData = await context.FetchStateAsync<Data<int, string>>() ?? new Data<int, string> {Value = 0};
            for (var i = lastData.Value; i < count; i++)
            {
                await context.UpdateStateAsync(lastData);
                logger.LogInformation($"[{tag}] Generator generates: {i}");
                if(tag == "first") await Task.Delay(TimeSpan.FromMilliseconds(1000), cancellationToken);
                lastData = new Data<int, string> {Value = i};
                await output.AddAsync(lastData, cancellationToken);
            }
        }
    }
}