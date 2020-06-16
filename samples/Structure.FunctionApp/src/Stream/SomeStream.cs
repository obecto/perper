using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Structure.FunctionApp.Stream
{
    public class SomeStream
    {
        [FunctionName(nameof(SomeStream))]
        public static async void Run([PerperStreamTrigger] PerperStreamContext context,
            [PerperStream("input")] IAsyncEnumerable<string> input,
            [PerperStream("output")] IAsyncCollector<int> output)
        {
            await foreach(var item in input)
            {
                await output.AddAsync(int.Parse(item));
            }
        }
    }
}