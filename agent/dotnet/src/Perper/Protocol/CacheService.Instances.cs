using System.Threading.Tasks;

using Perper.Protocol.Instance;

namespace Perper.Protocol
{
    public partial class CacheService
    {
        public Task InstanceCreate(string instance, string agent)
        {
            var instanceData = new InstanceData(agent);

            return instancesCache.PutIfAbsentOrThrowAsync(instance, instanceData);
        }

        public Task InstanceDestroy(string instance)
        {
            return instancesCache.RemoveAsync(instance);
        }
    }
}