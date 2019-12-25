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
        public static IAsyncEnumerable<IEnumerable<(T, IBinaryObject)>> QueryContinuousAsync<T>(
            this ICache<T, IBinaryObject> cache,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            return QueryContinuousAsync(cache, null, cancellationToken);
        }
        
        public static async IAsyncEnumerable<IEnumerable<(T, IBinaryObject)>> QueryContinuousAsync<T>(
            this ICache<T, IBinaryObject> cache,
            Func<T, IBinaryObject, bool> filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<IEnumerable<(T, IBinaryObject)>>();
            var listener = new Listener<T>(channel);
            using var queryHandle = filter == null
                ? cache.QueryContinuous(new ContinuousQuery<T, IBinaryObject>(listener))
                : cache.QueryContinuous(new ContinuousQuery<T, IBinaryObject>(listener),
                    new ScanQuery<T, IBinaryObject>(new Filter<T>(filter)));
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

        private class Filter<T> : ICacheEntryFilter<T, IBinaryObject>
        {
            private readonly Func<T, IBinaryObject, bool> _callback;

            public Filter(Func<T, IBinaryObject, bool> callback)
            {
                _callback = callback;
            }
            
            public bool Invoke(ICacheEntry<T, IBinaryObject> entry)
            {
                return _callback(entry.Key, entry.Value);
            }
        }
    }
}