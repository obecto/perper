using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Event;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Cache.Query.Continuous;

namespace Perper.Fabric
{
    public static class IgniteCacheExtensions
    {
        public static Task PutAllAsync<TK, TV>(this ICache<TK, TV> cache, IEnumerable<(TK, TV)> values)
        {
            return cache.PutAllAsync(values.Select(v => new KeyValuePair<TK, TV>(v.Item1, v.Item2)));
        }

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

        public static async IAsyncEnumerable<IEnumerable<(TK, TV)>> QueryContinuousAsync<TK, TV>(
            this ICache<TK, TV> cache,
            TK filterKey,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) where TK : IComparable
        {
            var channel = Channel.CreateUnbounded<IEnumerable<(TK, TV)>>();
            var listener = new Listener<TK, TV>(channel);
            var filter = new Filter<TK, TV>(filterKey);
            using var queryHandle = cache.QueryContinuous(new ContinuousQuery<TK, TV>(listener, filter),
                new ScanQuery<TK, TV>(filter));
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

        private class Filter<TK, TV> : ICacheEntryFilter<TK, TV>, ICacheEntryEventFilter<TK, TV>
            where TK : IComparable
        {
            private readonly TK _filterKey;

            public Filter(TK filterKey)
            {
                _filterKey = filterKey;
            }

            public bool Invoke(ICacheEntry<TK, TV> entry)
            {
                return _filterKey.Equals(entry.Key);
            }

            public bool Evaluate(ICacheEntryEvent<TK, TV> evt)
            {
                return _filterKey.Equals(evt.Key);
            }
        }
    }
}