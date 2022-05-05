using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Client.Cache;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public ICacheClient<int, T> GetListCache<T>(string instance, string name)
        {
            var queryEntity = new QueryEntity(typeof(int), typeof(T));

            return Ignite.GetOrCreateCache<int, T>(new CacheClientConfiguration($"{instance}-list-{name}", queryEntity));
        }

        public ICacheClient<string, T> GetListMetaCache<T>(string instance, string name)
        {
            var queryEntity = new QueryEntity(typeof(string), typeof(T));

            return Ignite.GetOrCreateCache<string, T>(new CacheClientConfiguration($"{instance}-list-{name}-meta", queryEntity));
        }

        public ICacheClient<TKey, TValue> GetDictionaryCache<TKey, TValue>(string instance, string name)
        {
            var queryEntity = new QueryEntity(typeof(TKey), typeof(TValue));

            return Ignite.GetOrCreateCache<TKey, TValue>(new CacheClientConfiguration($"{instance}-dictionary-{name}", queryEntity));
        }
    }
}