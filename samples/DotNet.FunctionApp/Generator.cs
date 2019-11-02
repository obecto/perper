using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Generator
    {
        [FunctionName("Generator")]
        public static void Run([PerperStreamTrigger] IPerperStreamContext context,
            [PerperStream("count")] int count,
            [PerperStream] IAsyncCollector<int> output)
        {
        }
    }
}