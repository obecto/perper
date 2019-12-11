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
        public static async Task Run([PerperTrigger("Processor")] IPerperStreamContext context,
            [Perper("generator")] IAsyncEnumerable<int> generator,
            [Perper("multiplier")] int multiplier,
            [Perper("output")] IAsyncCollector<int> output)
        {
            var state = context.GetState<List<int>>();
            await foreach (var value in generator)
            {
                var result = await context.CallWorkerFunction<int>(new {state, value, multiplier});
                state.Add(result);
                await context.SaveState();
                await output.AddAsync(result);
            }
        }

        [FunctionName("Worker")]
        [return: Perper]
        public static int Worker([PerperTrigger("Processor", "state")] IEnumerable<int> state,
            [Perper("value")] int value,
            [Perper("multiplier")] int multiplier)
        {
            return state.Last() + value * multiplier;
        }
    }
}