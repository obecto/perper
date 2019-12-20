using System.Collections.Generic;
using System.Linq;
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
            [Perper("generator")] IAsyncEnumerable<int> generator,
            [Perper("multiplier")] int multiplier,
            [Perper("output")] IAsyncCollector<int> output)
        {
            var state = await context.FetchStateAsync<List<int>>();
            await foreach (var value in generator)
            {
                var result = await context.CallWorkerAsync<int>(new {value, multiplier, state});
                state.Add(result);
                await context.UpdateStateAsync(state);
                await output.AddAsync(result);
            }
        }

        [FunctionName("Worker")]
        [return: Perper("$return")]
        public static int Worker([PerperWorkerTrigger("Processor")] IPerperWorkerContext context,
            [Perper("value")] int value,
            [Perper("multiplier")] int multiplier,
            [Perper("state")] IEnumerable<int> state)
        {
            return state.Last() + value * multiplier;
        }
    }
}