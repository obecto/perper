using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

namespace Perper.WebJobs.Extensions
{
    public static class PerperIgniteClientExtensions
    {
        public static ICacheClient<T, IBinaryObject> GetBinaryCache<T>(this IIgniteClient igniteClient, string name)
        {
            return igniteClient.GetCache<T, object>(name).WithKeepBinary<T, IBinaryObject>();
        }
    }
}