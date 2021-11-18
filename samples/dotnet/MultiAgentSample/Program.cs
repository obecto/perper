using System;
using System.Threading.Tasks;

using MultiAgentSample;

using Perper.Application;
using Perper.Extensions;
using Perper.Model;

static async Task Init()
{
    var generator = await PerperContext.StartAgentAsync(nameof(GeneratorAgent), 100).ConfigureAwait(false);
    var generatorStream = await generator.CallAsync<PerperStream>("GetStream").ConfigureAwait(false);
    var processor = await PerperContext.StartAgentAsync(nameof(ProcessorAgent), generatorStream).ConfigureAwait(false);
    var processorStream = await processor.CallAsync<PerperStream>("GetStream").ConfigureAwait(false);

    await foreach (var i in processorStream.EnumerateAsync<int>())
    {
        Console.WriteLine(i);
    }
}

await new PerperStartup()
    .AddClassHandlers(typeof(GeneratorAgent))
    .AddClassHandlers(typeof(ProcessorAgent))
    .AddInitHandler("", Init)
    .RunAsync(default).ConfigureAwait(false);