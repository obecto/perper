using System;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client.Cache;

namespace Perper.Protocol.Extensions
{
    public static class PerperContextExtensions
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