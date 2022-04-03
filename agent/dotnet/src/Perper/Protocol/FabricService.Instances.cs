using System;
using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperAgents
    {
        private IPerperExecutions PerperExecutions => this;

        (PerperAgent Instance, DelayedCreateFunc Start) IPerperAgents.Create(string agent)
        {
            var (instance, create) = CreateWithoutStarting(agent);
            return (instance, async (arguments) =>
            {
                await create().ConfigureAwait(false);
                await PerperExecutions.CallAsync(instance, PerperAgentsExtensions.StartupFunctionName, arguments).ConfigureAwait(false);
            }
            );
        }

        (PerperAgent Instance, DelayedCreateFunc<TResult> Start) IPerperAgents.Create<TResult>(string agent)
        {
            var (instance, create) = CreateWithoutStarting(agent);
            return (instance, async (arguments) =>
            {
                await create().ConfigureAwait(false);
                return await PerperExecutions.CallAsync<TResult>(instance, PerperAgentsExtensions.StartupFunctionName, arguments).ConfigureAwait(false);
            }
            );
        }

        private (PerperAgent Instance, Func<Task> Create) CreateWithoutStarting(string agent)
        {
            var instance = new PerperAgent(agent, GenerateName(agent));
            return (instance, async () =>
            {
                await _instancesCache.PutIfAbsentOrThrowAsync(instance.Instance, new InstanceData(instance.Agent)).ConfigureAwait(false);
            }
            );
        }

        async Task IPerperAgents.DestroyAsync(PerperAgent agent) => await _instancesCache.RemoveAsync(agent.Instance).ConfigureAwait(false);
    }
}