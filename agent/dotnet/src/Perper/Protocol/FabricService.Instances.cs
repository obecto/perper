using System.Threading.Tasks;
using System;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public async Task CreateInstance(string instance, string agent)
        {
            await CreateExecution(instance, "Registry", agent, "Run", Array.Empty<object>()).ConfigureAwait(false);
            // var instanceData = new InstanceData(agent);
            // await InstancesCache.PutIfAbsentOrThrowAsync(instance, instanceData).ConfigureAwait(false);
        }

        public async Task RemoveInstance(string instance)
        {
            await RemoveExecution(instance).ConfigureAwait(false);
            //await InstancesCache.RemoveAsync(instance).ConfigureAwait(false);
        }
    }
}