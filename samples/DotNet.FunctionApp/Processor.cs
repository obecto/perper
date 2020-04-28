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
    public static class Processor
    {
        [FunctionName("Processor")]
        public static async Task RunAsync([PerperStreamTrigger] PerperStreamContext context,
            [PerperStream("generator")] IAsyncEnumerable<Data<int, string>> generator,
            [Perper("multiplier")] int multiplier,
            [PerperStream("output")] IAsyncCollector<Data<int, string>> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            var state = await context.FetchStateAsync<List<int>>() ?? new List<int>();
            await foreach (var data in generator.WithCancellation(cancellationToken))
            {
                var value = data.Value;
                logger.LogInformation($"Processor is processing value: {value}");

                var result = await context.CallWorkerAsync<int>(typeof(Worker), new {value, multiplier, state}, cancellationToken);
                state.Add(result);
                await context.UpdateStateAsync(state);
                
                await output.AddAsync(new Data<int, string>{Value = result}, cancellationToken);
            }
        }
    }
}