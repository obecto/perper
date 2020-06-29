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
            await using var multiGenerator =
                await context.StreamFunctionAsync("NamedGeneratorGenerator", typeof(GeneratorGenerator), new {count = 2});
            await using var multiProcessor =
                await context.StreamFunctionAsync("NamedMultiProcessor", typeof(MultiProcessor), new {generators = multiGenerator});
            await using var coallator =
                await context.StreamActionAsync("NamedCoallator", typeof(Coallator), new {inputs = multiProcessor});
            await using var consumer =
                await context.StreamActionAsync("NamedPassthroughConsumer", typeof(PassthroughConsumer), new {processor = coallator.GetRef()});

            await context.BindOutput(cancellationToken);
        }
    }
}