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
    public static class Coallator
    {
        [FunctionName("Coallator")]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("inputs")] IAsyncEnumerable<IPerperStream> inputs,
            ILogger logger, CancellationToken cancellationToken)
        {
            var outputs = new List<IPerperStream>();
            await foreach (var input in inputs.WithCancellation(cancellationToken))
            {
                logger.LogInformation($"Coallator receives input");
                outputs.Add(input);
                await context.RebindOutput(outputs);
            }
        }
    }
}