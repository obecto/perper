using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperAgentsExtensions
    {
        public static string StartupFunctionName => "Start"; // TODO: Move out of hereFabricList
        public static string StopFunctionName => "Stop"; // TODO: Move out of here

        public static async Task<PerperAgent> CreateAsync(this IPerperAgents perperAgents, PerperAgent? parent, string @delegate, params object?[] arguments)
        {
            var (instance, delayedCreate) = perperAgents.Create(parent, @delegate);
            await delayedCreate(arguments).ConfigureAwait(false);
            return instance;
        }

        public static async Task<(PerperAgent instance, TResult result)> CreateAsync<TResult>(this IPerperAgents perperAgents, PerperAgent? parent, string @delegate, params object?[] arguments)
        {
            var (instance, delayedCreate) = perperAgents.Create<TResult>(parent, @delegate);
            var result = await delayedCreate(arguments).ConfigureAwait(false);
            return (instance, result);
        }

        public static Task<PerperAgent> CreateAgentAsync(this IPerperContext perper, string @delegate, params object?[] arguments) =>
            perper.Agents.CreateAsync(perper.CurrentAgent, @delegate, arguments);

        public static Task<(PerperAgent instance, TResult result)> CreateAgentAsync<TResult>(this IPerperContext perper, string @delegate, params object?[] arguments) =>
            perper.Agents.CreateAsync<TResult>(perper.CurrentAgent, @delegate, arguments);
    }
}