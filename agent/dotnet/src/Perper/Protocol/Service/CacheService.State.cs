using System;
using System.Threading.Tasks;

namespace Perper.Protocol.Service
{
    public partial class CacheService
    {
        public async Task<(bool, T)> StateTryGetValue<T>(string instance, string key)
        {
            var stateCache = Ignite.GetOrCreateCache<string, T>(instance);
            var result = await stateCache.TryGetAsync(key).ConfigureAwait(false);
            if (!result.Success)
            {
                return (false, default(T));
            }
            return (true, result.Value);
        }

        public Task StateSetValue<T>(string instance, string key, T value)
        {
            var stateCache = Ignite.GetOrCreateCache<string, T>(instance);
            return stateCache.PutAsync(key, value);
        }
    }
}