using System.Threading.Tasks;

using Apache.Ignite.Core.Client.Cache;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public ICacheClient<string, T> GetCollectionCache<T>(string instance, string name)
        {
            return Ignite.GetOrCreateCache<string, T>($"{instance}-collections-{name}");
        }

        public async Task<(bool Exists, T Value)> TryGetCollectionValue<T>(string instance, string name, string key)
        {
            var result = await GetCollectionCache<T>(instance, name).TryGetAsync(key).ConfigureAwait(false);
            if (!result.Success)
            {
                return (false, default(T)!);
            }

            return (true, result.Value);
        }

        public async Task SetCollectionValue<T>(string instance, string name, string key, T value)
        {
            await GetCollectionCache<T>(instance, name).PutAsync(key, value).ConfigureAwait(false);
        }

        public async Task RemoveCollectionValue<T>(string instance, string name, string key)
        {
            await GetCollectionCache<T>(instance, name).RemoveAsync(key).ConfigureAwait(false);
        }
    }
}
