using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperAgentsExtensions
    {
        public static string StartupFunctionName => "Startup"; // TODO: Move out of here

        public static async Task<PerperAgent> CreateAsync(this IPerperAgents perperAgents, string @delegate, params object?[] arguments)
        {
            var (instance, delayedCreate) = perperAgents.Create(@delegate);
            await delayedCreate(arguments).ConfigureAwait(false);
            return instance;
        }

        public static async Task<(PerperAgent instance, TResult result)> CreateAsync<TResult>(this IPerperAgents perperAgents, string @delegate, params object?[] arguments)
        {
            var (instance, delayedCreate) = perperAgents.Create<TResult>(@delegate);
            var result = await delayedCreate(arguments).ConfigureAwait(false);
            return (instance, result);
        }
    }
}