using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Client.Cache;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public ICacheClient<int, T> GetListCache<T>(string instance, string name, bool keepBinary = false)
        {
            return GetCollectionCache<int, T>($"{instance}-list-{name}").WithKeepBinary(keepBinary);
        }

        public ICacheClient<string, T> GetListMetaCache<T>(string instance, string name)
        {
            return GetCollectionCache<string, T>($"{instance}-list-{name}-meta");
        }

        public ICacheClient<TKey, TValue> GetDictionaryCache<TKey, TValue>(string instance, string name, bool keepBinary = false)
        {
            return GetCollectionCache<TKey, TValue>($"{instance}-dictionary-{name}").WithKeepBinary(keepBinary);
        }

        private ICacheClient<TK, TV> GetCollectionCache<TK, TV>(string name)
        {
            var queryEntity = new QueryEntity(typeof(TK), typeof(TV));

            if (queryEntity.Fields == null)
            {
                return Ignite.GetOrCreateCache<TK, TV>(name);
            }
            else
            {
                return Ignite.GetOrCreateCache<TK, TV>(new CacheClientConfiguration(name, queryEntity));
            }
        }
    }
}