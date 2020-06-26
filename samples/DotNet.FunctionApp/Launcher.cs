using System.Threading;
using System.Threading.Tasks;
using DotNet.FunctionApp.Model;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("Launcher")]
        public static async Task RunAsync([PerperStreamTrigger(RunOnStartup = true)]
            PerperStreamContext context,
            CancellationToken cancellationToken)
        {
            await using var firstGenerator =
                await context.StreamFunctionAsync("NamedGenerator", typeof(Generator), new {count = 10, tag = "first"}, typeof(Data));
            await using var processor =
                await context.StreamFunctionAsync("NamedProcessor", typeof(Processor), new
                {
                    generator = new[] {firstGenerator},
                    multiplier = 10
                }, typeof(Data));
            await using var consumer =
                await context.StreamActionAsync("NamedPassthroughConsumer", typeof(PassthroughConsumer), new {processor = processor.GetRef()});

            await context.BindOutput(cancellationToken);
        }
    }
}