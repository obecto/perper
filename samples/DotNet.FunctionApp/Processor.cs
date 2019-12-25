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
        public static async Task RunAsync([PerperStreamTrigger("Processor")] IPerperStreamContext context,
            [PerperStream("generator")] IAsyncEnumerable<Data> generator,
            [Perper("multiplier")] int multiplier,
            [PerperStream("output")] IAsyncCollector<Data> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            // var state = await context.FetchStateAsync<List<int>>();
            await foreach (var data in generator.WithCancellation(cancellationToken))
            {
                // var result = await context.CallWorkerAsync<int>(new {value, multiplier, state}, cancellationToken);
                // state.Add(result);
                // await context.UpdateStateAsync(state);
                logger.LogInformation($"Processor is processing value: {data.Value}");
                await output.AddAsync(new Data{Value = data.Value * 2}, cancellationToken);
            }
        }
    }
}