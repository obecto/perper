using System.Collections.Generic;
using System.Linq;
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
            [PerperStream("generator")] IAsyncEnumerable<Data> generator,
            [Perper("multiplier")] int multiplier,
            [PerperStream("output")] IAsyncCollector<Data> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            var state = await context.FetchStateAsync<List<int>>() ?? new List<int>();

            int counter = 0;
            await foreach (var data in generator.WithCancellation(cancellationToken))
            {
                var value = data.Value;
                logger.LogInformation($"Processor is processing value: {value}");

                var result = await context.CallWorkerAsync<int>(typeof(Worker), new { value, multiplier, state }, cancellationToken);
                state.Add(result);
                await context.UpdateStateAsync(state);

                await output.AddAsync(new Data { Value = result, Description = $"Description {result}" }, cancellationToken);
                counter++;

                if (counter > 4)
                {
                    var iquery = context.Query<Data>(generator).Where(item => item.Value > 100);

                    foreach (var item in iquery.ToList())
                    {
                        logger.LogInformation($"LINQ Query (Where): Received odd item with value: {item.Value}");
                    }
                }
            }
        }
    }
}