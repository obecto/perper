using System;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperAgents
    {
        private IPerperExecutions PerperExecutions => this;
        private IPerperStates PerperStates => this;

        (PerperAgent Instance, DelayedCreateFunc Start) IPerperAgents.Create(PerperAgent? parent, string agent)
        {
            var (instance, create) = CreateWithoutStarting(parent, agent);
            return (instance, async (arguments) =>
            {
                await create().ConfigureAwait(false);
                await PerperExecutions.CallAsync(instance, PerperAgentsExtensions.StartFunctionName, arguments).ConfigureAwait(false);
            }
            );
        }

        (PerperAgent Instance, DelayedCreateFunc<TResult> Start) IPerperAgents.Create<TResult>(PerperAgent? parent, string agent)
        {
            var (instance, create) = CreateWithoutStarting(parent, agent);
            return (instance, async (arguments) =>
            {
                await create().ConfigureAwait(false);
                return await PerperExecutions.CallAsync<TResult>(instance, PerperAgentsExtensions.StartFunctionName, arguments).ConfigureAwait(false);
            }
            );
        }

        private (PerperAgent Instance, Func<Task> Create) CreateWithoutStarting(PerperAgent? parent, string agent)
        {
            var (execution, start) = PerperExecutions.Create(new PerperAgent("Registry", agent), "Run", null);
            var instance = new PerperAgent(agent, execution.Execution);
            return (instance, async () =>
            {
                // await InstancesCache.PutIfAbsentOrThrowAsync(instance.Instance, new InstanceData(instance.Agent)).ConfigureAwait(false);
                if (parent != null)
                {
                    await (await GetChildren(parent).ConfigureAwait(false))
                        .AddAsync(instance.Instance, instance.Agent)
                        .ConfigureAwait(false);
                }
                await start().ConfigureAwait(false);
            }
            );
        }

        async Task IPerperAgents.DestroyAsync(PerperAgent instance)
        {
            await PerperExecutions.CallAsync(instance, PerperAgentsExtensions.StopFunctionName).ConfigureAwait(false);

            await foreach (var child in await GetChildren(instance).ConfigureAwait(false)) // TODO: Move to the implementation for Stop() instead of managing the agent's children directly
            {
                await ((IPerperAgents)this).DestroyAsync(new PerperAgent(child.Value, child.Key)).ConfigureAwait(false);
            }

            var execution = new PerperExecution(instance.Instance);
            await PerperExecutions.DestroyAsync(execution).ConfigureAwait(false);
            // await InstancesCache.RemoveAsync(instance.Instance).ConfigureAwait(false);
        }

        private async Task<IAsyncDictionary<string, string>> GetChildren(PerperAgent instance) =>
            PerperStates.AsAsyncDictionary<string, string>(await PerperStates.CreateAsync(instance, "children").ConfigureAwait(false));
    }
}