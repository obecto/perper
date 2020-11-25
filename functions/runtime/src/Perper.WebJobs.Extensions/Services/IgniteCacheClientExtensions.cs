using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Client.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public static class IgniteCacheClientExtensions
    {
        private static HashSet<Type> _ignitePrimitiveTypes = new HashSet<Type> {
            typeof(bool), typeof(char), typeof(byte), typeof(short), typeof(int), typeof(long),
            typeof(float), typeof(double), typeof(decimal), typeof(DateTime), typeof(Guid),
            typeof(bool[]), typeof(char[]), typeof(byte[]), typeof(short[]), typeof(int[]), typeof(long[]),
            typeof(float[]), typeof(double[]), typeof(decimal[]), typeof(DateTime[]), typeof(Guid[])
        };

        public static async Task<V> GetWithServicesAsync<K, V>(this ICacheClient<K, V> cache, K key, IServiceProvider services)
        {
            if (_ignitePrimitiveTypes.Contains(typeof(V)))
            {
                return await cache.GetAsync(key);
            }

            var value = await cache.WithKeepBinary<K, IBinaryObject>().GetAsync(key);

            return ConvertResult<V>(value, services);
        }

        public static async Task<CacheResult<V>> TryGetWithServicesAsync<K, V>(this ICacheClient<K, V> cache, K key, IServiceProvider services)
        {
            if (_ignitePrimitiveTypes.Contains(typeof(V)))
            {
                return await cache.TryGetAsync(key);
            }

            var result = await cache.WithKeepBinary<K, IBinaryObject>().TryGetAsync(key);

            return ConvertResult<V>(result, services);
        }

        public static async Task<CacheResult<V>> GetAndRemoveWithServicesAsync<K, V>(this ICacheClient<K, V> cache, K key, IServiceProvider services)
        {
            if (_ignitePrimitiveTypes.Contains(typeof(V)))
            {
                return await cache.GetAndRemoveAsync(key);
            }

            var result = await cache.WithKeepBinary<K, IBinaryObject>().GetAndRemoveAsync(key);

            return ConvertResult<V>(result, services);
        }

        private static V ConvertResult<V>(IBinaryObject value, IServiceProvider services)
        {
            PerperBinarySerializer.Services = services;
            var result = value.Deserialize<V>();
            PerperBinarySerializer.Services = null;

            return result;
        }

        private static CacheResult<V> ConvertResult<V>(CacheResult<IBinaryObject> result, IServiceProvider services)
        {
            if (result.Success)
            {
                return new CacheResult<V>(ConvertResult<V>(result.Value, services));
            }
            else
            {
                return new CacheResult<V>();
            }
        }
    }
}
