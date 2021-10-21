using System.Threading.Tasks;

using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public partial class CacheService
    {
        public Task CreateInstance(string instance, string agent)
        {
            var instanceData = new InstanceData(agent);

            return instancesCache.PutIfAbsentOrThrowAsync(instance, instanceData);
        }

        public Task RemoveInstance(string instance)
        {
            return instancesCache.RemoveAsync(instance);
        }
    }
}