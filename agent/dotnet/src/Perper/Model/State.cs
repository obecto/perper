using System;
using System.Threading.Tasks;

namespace Perper.Model
{
    public class State : IState
    {
        public async Task<(bool, T)> TryGetAsync<T>(string key)
        {
            return await AsyncLocals.CacheService.StateTryGetValue<T>(AsyncLocals.Instance, key).ConfigureAwait(false);
        }

        [Obsolete("Use idempotent operations instead")]
        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> defaultValueFactory)
        {
            var (present, value) = await AsyncLocals.CacheService.StateTryGetValue<T>(AsyncLocals.Instance, key).ConfigureAwait(false);
            if (!present)
            {
                value = await defaultValueFactory().ConfigureAwait(false);
                await AsyncLocals.CacheService.StateSetValue(AsyncLocals.Instance, key, value).ConfigureAwait(false);
            }
            return value;
        }

        public async Task SetAsync<T>(string key, T value)
        {
            await AsyncLocals.CacheService.StateSetValue(AsyncLocals.Instance, key, value).ConfigureAwait(false);
        }
    }
}