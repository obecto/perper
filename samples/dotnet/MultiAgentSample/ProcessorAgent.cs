using System.Collections.Generic;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace MultiAgentSample
{
    public static class ProcessorAgent
    {
        public static async Task StartupAsync(PerperStream input)
        {
            var stream = await PerperContext.Stream("Process").StartAsync(input).ConfigureAwait(false);
            await PerperState.SetAsync("stream", stream).ConfigureAwait(false);
        }

        public static async IAsyncEnumerable<int> ProcessAsync(PerperStream input)
        {
            var accumulator = await PerperState.GetOrDefaultAsync("accumulator", 0).ConfigureAwait(false);

            await foreach (var i in input.EnumerateAsync<int>())
            {
                accumulator += i;
                await PerperState.SetAsync("accumulator", accumulator).ConfigureAwait(false);
                yield return accumulator;
            }
        }

        public static Task<PerperStream> GetStreamAsync() =>
            PerperState.GetOrDefaultAsync<PerperStream>("stream");
    }
}