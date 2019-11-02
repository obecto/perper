using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Consumer
    {
        [FunctionName("Consumer")]
        public static void Run([PerperStreamTrigger] IPerperStreamContext context,
            [PerperStream("processor")] IPerperStream<int> processor)
        {
        }
    }
}