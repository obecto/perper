using System.Collections.Generic;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;
using PerperState = Perper.Extensions.PerperState;

namespace MultiAgentSample
{
    public static class GeneratorAgent
    {
        public static async Task StartupAsync(int count)
        {
            var stream = await PerperContext.Stream("Generate").Persistent().StartAsync(count).ConfigureAwait(false);
            await PerperState.SetAsync("stream", stream.Replay()).ConfigureAwait(false);
        }

#pragma warning disable 1998
        public static async IAsyncEnumerable<int> GenerateAsync(int count)
        {
            var i = await PerperState.GetOrDefaultAsync("i", 0).ConfigureAwait(false);

            for (; i < count ; i++)
            {
                await PerperState.SetAsync("i", i).ConfigureAwait(false);
                yield return i;
                await Task.Delay(10).ConfigureAwait(false);
            }
        }
#pragma warning restore 1998

        public static Task<PerperStream> GetStreamAsync() =>
            PerperState.GetOrDefaultAsync<PerperStream>("stream");
    }
}