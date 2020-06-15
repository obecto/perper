using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Structure.FunctionApp
{
    public class Launcher
    {
        [FunctionName(nameof(StructureModule))]
        public static async void Run([PerperStreamTrigger] PerperStreamContext context,
            [PerperStream("input")] IAsyncEnumerable<object> input,
            [PerperStream("output")] IAsyncCollector<object> output)
        {
            await new PerperLoader().Load(new StructureModule(), context, input, output);
        }
    }
}