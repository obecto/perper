#if NETSTANDARD2_0
using System.Collections.Generic;
using System.Threading.Channels;

namespace Perper.WebJobs.Extensions.Fake
{
    public static class AsyncEnumerableExtensions
    {
#pragma warning disable CS1998
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                yield return item;
            }
        }
#pragma warning restore CS1998

        public static async IAsyncEnumerable<T> ReadAllAsync<T>(this ChannelReader<T> reader)
        {
            while (true)
            {
                yield return await reader.ReadAsync().ConfigureAwait(false);
            }
        }
    }
}
#endif