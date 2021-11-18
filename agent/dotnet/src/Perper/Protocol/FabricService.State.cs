using System.Threading.Tasks;

using Apache.Ignite.Core.Client.Cache;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public ICacheClient<TK, TV> GetStateCache<TK, TV>(string instance, string cache)
        {
            return Ignite.GetOrCreateCache<TK, TV>($"{instance}-{cache}");
        }

        public async Task<(bool Exists, T Value)> TryGetStateValue<T>(string instance, string key)
        {
            var result = await StatesCache.TryGetAsync($"{instance}-{key}").ConfigureAwait(false);
            if (!result.Success)
            {
                return (false, default(T)!);
            }
            return (true, (T)result.Value);
        }

        public async Task SetStateValue<T>(string instance, string key, T value)
        {
            if (value != null)
            {
                await StatesCache.PutAsync($"{instance}-{key}", value).ConfigureAwait(false);
            }
            else
            {
                await StatesCache.RemoveAsync($"{instance}-{key}").ConfigureAwait(false);
            }
        }
    }
}