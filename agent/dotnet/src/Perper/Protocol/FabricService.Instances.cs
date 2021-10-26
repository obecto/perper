using System.Threading.Tasks;

using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public async Task CreateInstance(string instance, string agent)
        {
            var instanceData = new InstanceData(agent);

            await InstancesCache.PutIfAbsentOrThrowAsync(instance, instanceData).ConfigureAwait(false);
        }

        public async Task RemoveInstance(string instance)
        {
            await InstancesCache.RemoveAsync(instance).ConfigureAwait(false);
        }
    }
}