using System;
using System.Threading.Tasks;

using MultiAgentSample;

using Perper.Application;
using Perper.Extensions;
using Perper.Model;
using PerperState = Perper.Extensions.PerperState;

static async Task Init()
{
    var (exists, processorStream) = await PerperState.TryGetAsync<PerperStream>("processor").ConfigureAwait(false);

    if (!exists)
    {
        var generator = await PerperContext.StartAgentAsync(nameof(GeneratorAgent), 100).ConfigureAwait(false);
        var generatorStream = await generator.CallAsync<PerperStream>("GetStream").ConfigureAwait(false);
        var processor = await PerperContext.StartAgentAsync(nameof(ProcessorAgent), generatorStream).ConfigureAwait(false);
        processorStream = await processor.CallAsync<PerperStream>("GetStream").ConfigureAwait(false);

        await PerperState.SetAsync("processor", processorStream).ConfigureAwait(false);
    }

    await foreach (var i in processorStream.EnumerateAsync<int>("processor"))
    {
        Console.WriteLine(i);
    }
}

await new PerperStartup()
    .AddClassHandlers(typeof(GeneratorAgent))
    .AddClassHandlers(typeof(ProcessorAgent))
    .AddInitHandler("", (Func<Task>)Init)
    .RunAsync(default).ConfigureAwait(false);