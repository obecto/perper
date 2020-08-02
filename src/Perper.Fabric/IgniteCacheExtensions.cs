using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Event;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Cache.Query.Continuous;

namespace Perper.Fabric
{
    public static class IgniteCacheExtensions
    {
        public static async IAsyncEnumerable<IEnumerable<(TK, TV)>> QueryContinuousAsync<TK, TV>(
            this ICache<TK, TV> cache,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) where TK : IComparable
        {
            var channel = Channel.CreateUnbounded<IEnumerable<(TK, TV)>>();
            var listener = new Listener<TK, TV>(channel);
            using var queryHandle = cache.QueryContinuous(new ContinuousQuery<TK, TV>(listener), new ScanQuery<TK, TV>());

            await channel.Writer.WriteAsync(queryHandle.GetInitialQueryCursor().GetAll()
                .Select(entry => (entry.Key, entry.Value)), cancellationToken);

            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }

        private class Listener<TK, TV> : ICacheEntryEventListener<TK, TV>
        {
            private readonly Channel<IEnumerable<(TK, TV)>> _channel;

            public Listener(Channel<IEnumerable<(TK, TV)>> channel)
            {
                _channel = channel;
            }

            public void OnEvent(IEnumerable<ICacheEntryEvent<TK, TV>> events)
            {
                _channel.Writer.TryWrite(events.Where(e => e.EventType != CacheEntryEventType.Removed)
                    .Select(e => (e.Key, e.Value)));
            }
        }
    }
}