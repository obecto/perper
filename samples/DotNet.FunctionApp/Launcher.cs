using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class Launcher
    {
        [FunctionName("Launcher")]
        public static async Task RunAsync([PerperStreamTrigger("Launcher", RunOnStartup = true)]
            IPerperStreamContext context, CancellationToken cancellationToken)
        {
            await using var generator = await context.StreamFunctionAsync("Generator", new {count = 100});
            await using var processor =
                await context.StreamFunctionAsync("Processor", new {generator, multiplier = 10});
            await using var consumer = await context.StreamActionAsync("Consumer", new {processor});

            await context.WaitUntilCancelled(cancellationToken);
        }
    }
}