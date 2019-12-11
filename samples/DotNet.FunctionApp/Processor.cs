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
        public static async Task Run([Perper(Stream = "Processor")] IPerperStreamContext context,
            [Perper("generator")] IAsyncEnumerable<int> generator,
            [Perper("multiplier")] int multiplier,
            [Perper("output")] IAsyncCollector<int> output)
        {
            await foreach (var value in generator)
            {
                await output.AddAsync(await context.CallWorkerFunction<int>(new {value, multiplier}));
            }
        }

        [FunctionName("Processor_Worker")]
        public static void Worker([Perper(Stream = "Processor")] IPerperWorkerContext context,
            [Perper("value")] int value,
            [Perper("multiplier")] int multiplier,
            [Perper("state", State = true)] List<int> state,
            [Perper("output")] out int output)
        {
            output = state.Last() + value * multiplier;
            state.Add(output);
        }
    }
}