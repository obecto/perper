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
            [PerperStream("output")] IAsyncCollector<Data> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            for (var i = 0; i < count; i++)
            {
                logger.LogInformation($"Generator generates: {i}");
                await output.AddAsync(new Data {Value = i}, cancellationToken);
            }
        }
    }
}