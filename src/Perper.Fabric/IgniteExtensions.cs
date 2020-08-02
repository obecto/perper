using Apache.Ignite.Core;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Configuration;

namespace Perper.Fabric
{
    public static class IgniteExtensions
    {
        public static ICache<T, object> GetBinaryCache<T>(this IIgnite ignite, string name)
        {
            return ignite.GetCache<T, object>(name).WithKeepBinary<T, object>();
        }
        
        public static ICache<T, object> CreateBinaryCache<T>(this IIgnite ignite, string name)
        {
            return ignite.CreateCache<T, object>(name).WithKeepBinary<T, object>();
        }

        public static ICache<T, object> CreateBinaryCache<T>(this IIgnite ignite, CacheConfiguration cacheConfiguration)
        {
            return ignite.CreateCache<T, object>(cacheConfiguration).WithKeepBinary<T, object>();
        }
    }
}