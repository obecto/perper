using Apache.Ignite.Core;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Configuration;

namespace Perper.Fabric
{
    public static class IgniteExtensions
    {
        public static ICache<T, object> GetOrCreateBinaryCache<T>(this IIgnite ignite, string name)
        {
            return ignite.GetOrCreateCache<T, object>(name).WithKeepBinary<T, object>();
        }

        public static ICache<T, object> GetOrCreateBinaryCache<T>(this IIgnite ignite, CacheConfiguration cacheConfiguration)
        {
            return ignite.GetOrCreateCache<T, object>(cacheConfiguration).WithKeepBinary<T, object>();
        }
    }
}