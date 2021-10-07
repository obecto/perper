using System.Threading.Tasks;

namespace Perper.Extensions
{
    public static class PerperState
    {
        public static async Task<(bool, T)> TryGetAsync<T>(string key)
        {
            return await AsyncLocals.CacheService.StateTryGetValue<T>(AsyncLocals.Instance, key).ConfigureAwait(false);
        }

        public static async Task SetAsync<T>(string key, T value)
        {
            await AsyncLocals.CacheService.StateSetValue(AsyncLocals.Instance, key, value).ConfigureAwait(false);
        }
    }
}