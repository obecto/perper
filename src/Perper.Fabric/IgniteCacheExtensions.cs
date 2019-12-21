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
        public static IAsyncEnumerable<IEnumerable<T>> GetKeysAsync<T>(this ICache<T, IBinaryObject> cache,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            return GetAsync<T, T>(cache, null, cancellationToken);
        }
        
        public static IAsyncEnumerable<IEnumerable<T>> GetKeysAsync<T>(this ICache<T, IBinaryObject> cache,
            Func<T, IBinaryObject, bool> filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            return GetAsync<T, T>(cache, filter, cancellationToken);
        }
        
        public static IAsyncEnumerable<IEnumerable<IBinaryObject>> GetValuesAsync<T>(this ICache<T, IBinaryObject> cache,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            return GetAsync<T, IBinaryObject>(cache, null, cancellationToken);
        }

        public static IAsyncEnumerable<IEnumerable<IBinaryObject>> GetValuesAsync<T>(
            this ICache<T, IBinaryObject> cache,
            Func<T, IBinaryObject, bool> filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            return GetAsync<T, IBinaryObject>(cache, filter, cancellationToken);
        }

        private static async IAsyncEnumerable<IEnumerable<TR>> GetAsync<TK, TR>(this ICache<TK, IBinaryObject> cache,
            Func<TK, IBinaryObject, bool> filter,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var channel = Channel.CreateUnbounded<IEnumerable<TR>>();
            var listener = new Listener<TK,TR>(channel);
            using var queryHandle = filter == null
                ? cache.QueryContinuous(new ContinuousQuery<TK, IBinaryObject>(listener))
                : cache.QueryContinuous(new ContinuousQuery<TK, IBinaryObject>(listener),
                    new ScanQuery<TK, IBinaryObject>(new Filter<TK>(filter)));
            await foreach (var result in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return result;
            }
        }

        private class Listener<T, TR> : ICacheEntryEventListener<T, IBinaryObject>
        {
            private readonly Channel<IEnumerable<TR>> _channel;

            public Listener(Channel<IEnumerable<TR>> channel)
            {
                _channel = channel;
            }

            public void OnEvent(IEnumerable<ICacheEntryEvent<T, IBinaryObject>> events)
            {
                _channel.Writer.TryWrite(events.Select(e =>
                {
                    if (e.Key is TR key)
                    {
                        return key;
                    }

                    if (e.Value is TR value)
                    {
                        return value;
                    }

                    throw new ArgumentException();
                }));
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