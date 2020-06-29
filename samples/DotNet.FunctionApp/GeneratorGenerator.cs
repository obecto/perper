using System;
using System.Threading;
using System.Threading.Tasks;
using DotNet.FunctionApp.Model;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace DotNet.FunctionApp
{
    public static class GeneratorGenerator
    {
        [FunctionName("GeneratorGenerator")]
        public static async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("count")] int count,
            [PerperStream("output")] IAsyncCollector<IPerperStream> output,
            ILogger logger, CancellationToken cancellationToken)
        {
            var state = await context.FetchStateAsync<Data>() ?? new Data {Value = 0};
            for (var i = state.Value; i < count; i++)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
                logger.LogInformation($"Generator Generator generates: {i}");
                var stream = await context.StreamFunctionAsync("NamedGenerator-" + i, typeof(Generator), new {count = 10, tag = "first" ?? "xx-" + i}, typeof(Data));
                await output.AddAsync(stream, cancellationToken);

                state.Value = i;
                await context.UpdateStateAsync(state);
            }
            await context.BindOutput(cancellationToken);
        }
    }
}