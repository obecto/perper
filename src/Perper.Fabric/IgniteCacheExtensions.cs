using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Event;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Cache.Query.Continuous;

namespace Perper.Fabric
{
    public static class IgniteCacheExtensions
    {
        public static async IAsyncEnumerable<IEnumerable<(T, IBinaryObject)>> QueryContinuousAsync<T>(
            this ICache<T, IBinaryObject> cache,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : IComparable
        {
            var channel = Channel.CreateUnbounded<IEnumerable<(T, IBinaryObject)>>();
            var listener = new Listener<T>(channel);
            using var queryHandle = cache.QueryContinuous(new ContinuousQuery<T, IBinaryObject>(listener));
            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }

        public static async IAsyncEnumerable<IEnumerable<(T, IBinaryObject)>> QueryContinuousAsync<T>(
            this ICache<T, IBinaryObject> cache,
            T filterKey,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : IComparable
        {
            var channel = Channel.CreateUnbounded<IEnumerable<(T, IBinaryObject)>>();
            var listener = new Listener<T>(channel);
            var filter = new Filter<T>(filterKey);
            using var queryHandle = cache.QueryContinuous(new ContinuousQuery<T, IBinaryObject>(listener, filter),
                new ScanQuery<T, IBinaryObject>(filter));
            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }

        private class Listener<T> : ICacheEntryEventListener<T, IBinaryObject>
        {
            private readonly Channel<IEnumerable<(T, IBinaryObject)>> _channel;

            public Listener(Channel<IEnumerable<(T, IBinaryObject)>> channel)
            {
                _channel = channel;
            }

            public void OnEvent(IEnumerable<ICacheEntryEvent<T, IBinaryObject>> events)
            {
                _channel.Writer.TryWrite(events.Select(e => (e.Key, e.Value)));
            }
        }

        private class Filter<T> : ICacheEntryFilter<T, IBinaryObject>, ICacheEntryEventFilter<T, IBinaryObject>
            where T : IComparable
        {
            private readonly T _filterKey;

            public Filter(T filterKey)
            {
                _filterKey = filterKey;
            }

            public bool Invoke(ICacheEntry<T, IBinaryObject> entry)
            {
                return _filterKey.Equals(entry.Key);
            }

            public bool Evaluate(ICacheEntryEvent<T, IBinaryObject> evt)
            {
                return _filterKey.Equals(evt.Key);
            }
        }
    }
}