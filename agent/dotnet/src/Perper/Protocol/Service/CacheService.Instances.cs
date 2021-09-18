using System.Threading.Tasks;

using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Extensions;

namespace Perper.Protocol.Service
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