using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperStreamsExtensions
    {
        public static PerperStream Replay(this PerperStream stream, bool replay = true) =>
            new(stream.Stream, replay ? 0 : -1, stream.Stride, stream.LocalToData);

        public static PerperStream Replay(this PerperStream stream, long replayFromKey) =>
            new(stream.Stream, replayFromKey, stream.Stride, stream.LocalToData);

        public static PerperStream LocalToData(this PerperStream stream, bool localToData = true) =>
            new(stream.Stream, stream.StartIndex, stream.Stride, localToData);

        public static async Task<PerperStream> CreateAsync(this IPerperStreams perperStreams, PerperStreamOptions options)
        {
            var (stream, delayedCreate) = perperStreams.Create(options);
            await delayedCreate().ConfigureAwait(false);
            return stream;
        }

        public static async Task<PerperStream> CreateAsync(this IPerperStreams perperStreams, PerperStreamOptions options, PerperExecution execution)
        {
            var (stream, delayedCreate) = perperStreams.Create(options, execution);
            await delayedCreate().ConfigureAwait(false);
            return stream;
        }

        /*public static PerperStream Filter<T>(this PerperStream stream, Expression<Func<T, bool>> filter) =>
            return new PerperStream(stream.Stream, FilterUtils.ConvertFilter(filter), stream.StartIndex, stream.Stride, stream.LocalToData);*/

        public static IAsyncEnumerable<TItem> EnumerateAsync<TItem>(this IPerperStreams streams, PerperStream stream, string? listenerName = null, CancellationToken cancellationToken = default) =>
            streams.EnumerateItemsAsync<TItem>(stream, listenerName, cancellationToken).Select(x => x.value);

        public static IAsyncEnumerable<TItem> EnumerateAsync<TItem>(this IPerper perper, PerperStream stream, string? listenerName = null, CancellationToken cancellationToken = default) =>
            perper.Streams.EnumerateAsync<TItem>(stream, listenerName, cancellationToken);

        public static IAsyncEnumerable<(long, TItem)> EnumerateItemsAsync<TItem>(this IPerper perper, PerperStream stream, string? listenerName = null, CancellationToken cancellationToken = default) =>
            perper.Streams.EnumerateItemsAsync<TItem>(stream, listenerName, cancellationToken);
    }
}