using System.Collections.Generic;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace MultiAgentSample
{
    public static class GeneratorAgent
    {
        public static async Task StartupAsync(int count)
        {
            var stream = await PerperContext.Stream("Generate").StartAsync(count).ConfigureAwait(false);
            await PerperState.SetAsync("stream", stream).ConfigureAwait(false);
        }

#pragma warning disable 1998
        public static async IAsyncEnumerable<int> GenerateAsync(int count)
        {
            for (var i = 0 ; i < count ; i++)
            {
                yield return i;
            }
        }
#pragma warning restore 1998

        public static Task<PerperStream> GetStreamAsync() =>
            PerperState.GetOrDefaultAsync<PerperStream>("stream");
    }
}