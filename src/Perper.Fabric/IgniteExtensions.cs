using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache;

namespace Perper.Fabric
{
    public static class IgniteExtensions
    {
        public static ICache<T, IBinaryObject> GetBinaryCache<T>(this IIgnite ignite, string name)
        {
            return ignite.GetCache<T, object>(name).WithKeepBinary<T, IBinaryObject>();
        }

        public static ICache<T, IBinaryObject> GetOrCreateBinaryCache<T>(this IIgnite ignite, string name)
        {
            return ignite.GetOrCreateCache<T, object>(name).WithKeepBinary<T, IBinaryObject>();
        }
    }
}