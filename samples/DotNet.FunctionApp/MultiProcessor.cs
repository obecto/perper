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
    public static class MultiProcessor
    {
        [FunctionName("MultiProcessor")]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("generators")] IAsyncEnumerable<IPerperStream> generators,
            [Perper("output")] IAsyncCollector<IPerperStream> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            var i = 0;
            await foreach (var generator in generators.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"Multi Processor receives generator: {0}", i);
                var stream = await context.StreamFunctionAsync("NamedProcessor-" + i, typeof(Processor), new {
                    generator = new[] { generator.Subscribe() },
                    multiplier = 10
                }, typeof(Data));
                await output.AddAsync(stream, cancellationToken);
                i += 1;
            }
        }
    }
}