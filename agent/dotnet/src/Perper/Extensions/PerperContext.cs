using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol.Instance;

namespace Perper.Extensions
{
    public static class PerperContext
    {
        public static string StartupFunctionName { get; } = "Startup";

        public static PerperAgent Agent => new(AsyncLocals.Agent, AsyncLocals.Instance);

        public static async Task<PerperAgent> StartAgentAsync(string agent, params object[] parameters)
        {
            var instance = await CreateInstanceAsync(agent).ConfigureAwait(false);
            await instance.CallActionAsync(StartupFunctionName, parameters).ConfigureAwait(false);
            return instance;
        }

        public static async Task<(PerperAgent, TResult)> StartAgentAsync<TResult>(string agent, params object[] parameters)
        {
            var instance = await CreateInstanceAsync(agent).ConfigureAwait(false);
            var result = await instance.CallFunctionAsync<TResult>(StartupFunctionName, parameters).ConfigureAwait(false);
            return (instance, result);
        }

        private static async Task<PerperAgent> CreateInstanceAsync(string agent)
        {
            var instance = AsyncLocals.CacheService.GenerateName(agent);
            await AsyncLocals.CacheService.InstanceCreate(instance, agent).ConfigureAwait(false);
            return new PerperAgent(agent, instance);
        }

        public static PerperStreamBuilder StreamAction(string @delegate)
        {
            return new PerperStreamBuilder(@delegate, StreamDelegateType.Action);
        }

        public static PerperStreamBuilder StreamFunction(string @delegate)
        {
            return new PerperStreamBuilder(@delegate, StreamDelegateType.Function);
        }

        public static PerperStreamBuilder BlankStream()
        {
            return new PerperStreamBuilder("", StreamDelegateType.External);
        }

        public static Task<TResult> CallFunctionAsync<TResult>(string functionName, params object[] parameters)
        {
            return Agent.CallFunctionAsync<TResult>(functionName, parameters);
        }

        public static Task CallActionAsync(string actionName, params object[] parameters)
        {
            return Agent.CallActionAsync(actionName, parameters);
        }
    }
}