using System.Threading.Tasks;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public async Task<(bool, T)> TryGetStateValue<T>(string instance, string key)
        {
            var stateCache = Ignite.GetOrCreateCache<string, T>(instance);
            var result = await stateCache.TryGetAsync(key).ConfigureAwait(false);
            if (!result.Success)
            {
                return (false, default(T)!);
            }
            return (true, result.Value);
        }

        public async Task SetStateValue<T>(string instance, string key, T value)
        {
            var stateCache = Ignite.GetOrCreateCache<string, T>(instance);
            await stateCache.PutAsync(key, value).ConfigureAwait(false);
        }

        public async Task RemoveStateValue(string instance, string key)
        {
            var stateCache = Ignite.GetOrCreateCache<string, object>(instance);
            await stateCache.RemoveAsync(key).ConfigureAwait(false);
        }
    }
}