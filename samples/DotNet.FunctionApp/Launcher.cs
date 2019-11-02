using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("Launcher")]
        public static void Run([PerperStreamTrigger] IPerperStreamContext context)
        {
            var generator = context.CallStreamFunction<int>("Generator", new {count = 100});
            var processor = context.CallStreamFunction<int>("Processor", new {generator, multiplier = 10});
            context.CallStreamAction("Consumer", new {processor});
        }
    }
}