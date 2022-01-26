using System;
using System.Threading.Tasks;

namespace Perper.Extensions
{
    public static class PerperState
    {
        public static async Task<(bool, T)> TryGetAsync<T>(string key)
        {
            return await AsyncLocals.FabricService.TryGetStateValue<T>(AsyncLocals.Instance, key).ConfigureAwait(false);
        }

        public static Task<T> GetOrDefaultAsync<T>(string key, T @default = default!) => GetOrNewAsync(key, () => @default);

        public static Task<T> GetOrNewAsync<T>(string key) where T : new() => GetOrNewAsync(key, () => new T());

        public static async Task<T> GetOrNewAsync<T>(string key, Func<T> createFunc)
        {
            var (success, value) = await TryGetAsync<T>(key).ConfigureAwait(false);
            return success ? value : createFunc();
        }

        public static async Task SetAsync<T>(string key, T value)
        {
            await AsyncLocals.FabricService.SetStateValue(AsyncLocals.Instance, key, value).ConfigureAwait(false);
        }

        public static async Task RemoveAsync(string key)
        {
            await AsyncLocals.FabricService.RemoveStateValue(AsyncLocals.Instance, key).ConfigureAwait(false);
        }
    }
}