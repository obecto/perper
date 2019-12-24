using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Processor
    {
        [FunctionName("Processor")]
        public static async Task RunAsync([PerperStreamTrigger("Processor")] IPerperStreamContext context,
            [PerperStream("generator")] IAsyncEnumerable<int> generator,
            [Perper("multiplier")] int multiplier,
            [PerperStream("output")] IAsyncCollector<int> output,
            CancellationToken cancellationToken)
        {
            var state = await context.FetchStateAsync<List<int>>();
            await foreach (var value in generator.WithCancellation(cancellationToken))
            {
                var result = await context.CallWorkerAsync<int>(new {value, multiplier, state}, cancellationToken);
                state.Add(result);
                await context.UpdateStateAsync(state);
                await output.AddAsync(result, cancellationToken);
            }
        }
    }
}