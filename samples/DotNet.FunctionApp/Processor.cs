using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Processor
    {
        [FunctionName("Processor")]
        public static async Task Run([PerperStreamTrigger] object state,
            [PerperStream("generator")] IPerperStream<int> generator,
            [PerperStream("multiplier")] int multiplier,
            [PerperStream] IAsyncCollector<int> output
        )
        {
            await Task.Delay(1);
        }
    }
}