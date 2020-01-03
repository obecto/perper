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
        public static async Task RunAsync([PerperStreamTrigger(RunOnStartup = true)] PerperStreamContext context,
            CancellationToken cancellationToken)
        {
            await using var firstGenerator = await context.StreamFunctionAsync(nameof(Generator), new {count = 10});
            await using var secondGenerator = await context.StreamFunctionAsync(nameof(Generator), new {count = 10});
            await using var processor =
                await context.StreamFunctionAsync(nameof(Processor), new
                {
                    generator = new[] {firstGenerator, secondGenerator},
                    multiplier = 10
                });
            await using var consumer = await context.StreamActionAsync(nameof(Consumer), new {processor});

            await context.BindOutput(cancellationToken);
        }
    }
}