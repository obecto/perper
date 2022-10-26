using System;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperAgents
    {
        private IPerperExecutions PerperExecutions => this;
        private IPerperStates PerperStates => this;

        (PerperInstance Instance, DelayedCreateFunc Start) IPerperAgents.Create(PerperInstance? parent, string agent)
        {
            var (instance, create) = CreateWithoutStarting(parent, agent);
            return (instance, async (arguments) =>
            {
                await create().ConfigureAwait(false);
                await PerperExecutions.CallAsync(instance, PerperAgentsExtensions.StartFunctionName, arguments).ConfigureAwait(false);
            }
            );
        }

        (PerperInstance Instance, DelayedCreateFunc<TResult> Start) IPerperAgents.Create<TResult>(PerperInstance? parent, string agent)
        {
            var (instance, create) = CreateWithoutStarting(parent, agent);
            return (instance, async (arguments) =>
            {
                await create().ConfigureAwait(false);
                return await PerperExecutions.CallAsync<TResult>(instance, PerperAgentsExtensions.StartFunctionName, arguments).ConfigureAwait(false);
            }
            );
        }

        private (PerperInstance Instance, Func<Task> Create) CreateWithoutStarting(PerperInstance? parent, string agent)
        {
            var (execution, start) = PerperExecutions.Create(new PerperInstance("Registry", agent), "Run", null);
            var instance = new PerperInstance(agent, execution.Execution);
            return (instance, async () =>
            {
                // await InstancesCache.PutIfAbsentOrThrowAsync(instance.Instance, new InstanceData(instance.Agent)).ConfigureAwait(false);
                if (parent != null)
                {
                    await PerperStates.AddAsync(PerperStates.GetInstanceChildrenList(parent), instance).ConfigureAwait(false);
                }
                await start().ConfigureAwait(false);
            }
            );
        }

        async Task IPerperAgents.DestroyAsync(PerperInstance instance)
        {
            await PerperExecutions.CallAsync(instance, PerperAgentsExtensions.StopFunctionName).ConfigureAwait(false);

            // TODO: Move to the implementation for Stop() instead of managing the agent's children directly
            await foreach (var child in PerperStates.EnumerateAsync<PerperInstance>(PerperStates.GetInstanceChildrenList(instance)).ConfigureAwait(false))
            {
                await ((IPerperAgents)this).DestroyAsync(child).ConfigureAwait(false);
            }

            var execution = new PerperExecution(instance.Instance);
            await PerperExecutions.DestroyAsync(execution).ConfigureAwait(false);
            // await InstancesCache.RemoveAsync(instance.Instance).ConfigureAwait(false);
        }
    }
}