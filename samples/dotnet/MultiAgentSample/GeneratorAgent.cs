#pragma warning disable IDE0005
#pragma warning disable CA1822

using System.Collections.Generic;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

using PerperState = Perper.Extensions.PerperState;

namespace MultiAgentSample;

public class GeneratorAgent
{
    public async Task StartupAsync(int count)
    {
        var stream = await PerperContext.Stream("Generate").Persistent().StartAsync(count).ConfigureAwait(false);
        await PerperState.SetAsync("stream", stream.Replay()).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<int> GenerateAsync(int count)
    {
        var i = await PerperState.GetOrDefaultAsync("i", 0).ConfigureAwait(false);

        for (; i < count ; i++)
        {
            await PerperState.SetAsync("i", i).ConfigureAwait(false);
            yield return i;
            await Task.Delay(10).ConfigureAwait(false);
        }
    }

    public Task<PerperStream> GetStreamAsync() => PerperState.GetOrDefaultAsync<PerperStream>("stream");
}

#pragma warning restore IDE0005
#pragma warning restore CA1822