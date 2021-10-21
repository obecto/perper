using System.Threading.Tasks;

namespace Perper.Extensions
{
    public static class PerperState
    {
        public static async Task<(bool, T)> TryGetAsync<T>(string key)
        {
            return await AsyncLocals.CacheService.TryGetStateValue<T>(AsyncLocals.Instance, key).ConfigureAwait(false);
        }

        public static async Task SetAsync<T>(string key, T value)
        {
            await AsyncLocals.CacheService.SetStateValue(AsyncLocals.Instance, key, value).ConfigureAwait(false);
        }
    }
}