using System;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client.Cache;

namespace Perper.Protocol
{
    public static class IgniteCacheExtensions
    {
        public static async Task OptimisticUpdateAsync<TK, TV>(this ICacheClient<TK, TV> cache, TK key, Func<TV, TV> updateFunc)
        {
            while (true)
            {
                var existingValue = await cache.GetAsync(key).ConfigureAwait(false);
                var newValue = updateFunc(existingValue);
                if (await cache.ReplaceAsync(key, existingValue, newValue).ConfigureAwait(false))
                {
                    break;
                }
            }
        }

        public static Task OptimisticUpdateAsync<TK, TV>(this ICacheClient<TK, TV> cache, TK key, IBinary binary, Action<TV> updateAction)
            where TV : class
        {
            return cache.WithKeepBinary<TK, IBinaryObject>().OptimisticUpdateAsync(key, (binaryObject) =>
            {
                var value = binaryObject.Deserialize<TV>();
                updateAction(value);
                return binary.ToBinary<IBinaryObject>(value);
            });
        }

        public static ICacheClient<TK, TV> WithKeepBinary<TK, TV>(this ICacheClient<TK, TV> cache, bool keepBinary)
        {
            return keepBinary ? cache.WithKeepBinary<TK, TV>() : cache;
        }

        public static async Task PutIfAbsentOrThrowAsync<TK, TV>(this ICacheClient<TK, TV> cache, TK key, TV value)
        {
            var result = await cache.PutIfAbsentAsync(key, value).ConfigureAwait(false);
            if (!result)
            {
                throw new InvalidOperationException($"Duplicate cache item key! (key is {key})");
            }
        }
    }
}