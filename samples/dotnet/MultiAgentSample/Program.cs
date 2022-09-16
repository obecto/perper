using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Perper.Application;
using Perper.Extensions;
using Perper.Model;

namespace MultiAgentSample;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigurePerper(builder =>
            {
                builder
                    .AddClassHandlers<GeneratorAgent>()
                    .AddClassHandlers<ProcessorAgent>()
                    .AddHandler("MultiAgentSample", "Deploy", Deploy);
            }).ConfigureServices(services =>
            {
                if (args.Contains("--use-dataregions"))
                {
                    services.Configure<Perper.Protocol.FabricConfiguration>(o =>
                    {
                        o.PersistentStreamDataRegion = "persistent";
                        o.EphemeralStreamDataRegion = "ephemeral";
                        o.StateDataRegion = "persistent";
                    });
                }
            })
            .Build();

        await host.RunAsync().ConfigureAwait(false);
    }

    private static async Task Deploy()
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
}