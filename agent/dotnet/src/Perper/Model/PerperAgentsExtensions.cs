using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperAgentsExtensions
    {
        [System.Obsolete("Use StartFunctionName instead")]
        public static string StartupFunctionName => StartFunctionName; // TODO: Move out of here

        public static string StartFunctionName => "Start"; // TODO: Move out of here
        public static string StopFunctionName => "Stop"; // TODO: Move out of here

        public static async Task<PerperInstance> CreateAsync(this IPerperAgents perperAgents, PerperInstance? parent, string @delegate, params object?[] arguments)
        {
            var (instance, delayedCreate) = perperAgents.Create(parent, @delegate);
            await delayedCreate(arguments).ConfigureAwait(false);
            return instance;
        }

        public static async Task<(PerperInstance instance, TResult result)> CreateAsync<TResult>(this IPerperAgents perperAgents, PerperInstance? parent, string @delegate, params object?[] arguments)
        {
            var (instance, delayedCreate) = perperAgents.Create<TResult>(parent, @delegate);
            var result = await delayedCreate(arguments).ConfigureAwait(false);
            return (instance, result);
        }

        public static Task<PerperInstance> CreateAgentAsync(this IPerperContext perper, string @delegate, params object?[] arguments) =>
            perper.Agents.CreateAsync(perper.CurrentAgent, @delegate, arguments);

        public static Task<(PerperInstance instance, TResult result)> CreateAgentAsync<TResult>(this IPerperContext perper, string @delegate, params object?[] arguments) =>
            perper.Agents.CreateAsync<TResult>(perper.CurrentAgent, @delegate, arguments);
    }
}