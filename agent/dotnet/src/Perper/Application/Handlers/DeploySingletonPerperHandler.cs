using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public class DeploySingletonPerperHandler : IPerperHandler
    {
        private readonly IPerper Perper;

        public DeploySingletonPerperHandler(IServiceProvider services) =>
            Perper = services.GetRequiredService<IPerper>();

        public ParameterInfo[]? GetParameters() => null;

        public async Task Invoke(PerperExecutionData executionData, object?[] arguments)
        {
            var state = await Perper.States.CreateAsync(executionData.Agent).ConfigureAwait(false);
            var (exists, _) = await Perper.States.TryGetAsync<object?>(state, "agent").ConfigureAwait(false);
            if (!exists)
            {
                var (newAgent, createAsync) = Perper.Agents.Create(executionData.Agent, executionData.Agent.Instance);
                await Perper.States.SetAsync(state, "agent", newAgent).ConfigureAwait(false); // TODO: ensure this is atomic
                await createAsync().ConfigureAwait(false);
            }
        }
    }
}