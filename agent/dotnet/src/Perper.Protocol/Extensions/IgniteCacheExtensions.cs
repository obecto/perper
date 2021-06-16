using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client.Cache;

namespace Perper.Protocol.Extensions
{
    public static class PerperContextExtensions
    {
        public static async Task OptimisticUpdateAsync<K, V>(this ICacheClient<K, V> cache, K key, Func<V, V> updateFunc)
        {
            while (true)
            {
                var existingValue = await cache.GetAsync(key);
                var newValue = updateFunc(existingValue);
                if (await cache.ReplaceAsync(key, existingValue, newValue))
                {
                    break;
                }
            }
        }

        public static async Task PutIfAbsentOrThrowAsync<K, V>(this ICacheClient<K, V> cache, K key, V value)
        {
            var result = await cache.PutIfAbsentAsync(key, value);
            if (!result)
            {
                throw new Exception($"Duplicate cache item key! (key is {key})");
            }
        }
    }
}