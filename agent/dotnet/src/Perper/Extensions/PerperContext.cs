using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol;

namespace Perper.Extensions
{
    public static class PerperContext
    {
        public static string StartupFunctionName { get; } = "Startup";

        public static PerperAgent Agent => new(AsyncLocals.Agent, AsyncLocals.Instance);

        public static async Task<PerperAgent> StartAgentAsync(string agent, params object[] parameters)
        {
            var instance = await CreateInstanceAsync(agent).ConfigureAwait(false);
            await instance.CallAsync(StartupFunctionName, parameters).ConfigureAwait(false);
            return instance;
        }

        public static async Task<(PerperAgent, TResult)> StartAgentAsync<TResult>(string agent, params object[] parameters)
        {
            var instance = await CreateInstanceAsync(agent).ConfigureAwait(false);
            var result = await instance.CallAsync<TResult>(StartupFunctionName, parameters).ConfigureAwait(false);
            return (instance, result);
        }

        private static async Task<PerperAgent> CreateInstanceAsync(string agent)
        {
            var instance = CacheService.GenerateName(agent);
            await AsyncLocals.CacheService.InstanceCreate(instance, agent).ConfigureAwait(false);
            return new PerperAgent(agent, instance);
        }

        public static PerperStreamBuilder Stream(string @delegate)
        {
            return new PerperStreamBuilder(@delegate);
        }

        public static PerperStreamBuilder BlankStream()
        {
            return new PerperStreamBuilder(null);
        }

        public static Task<TResult> CallAsync<TResult>(string functionName, params object[] parameters) => Agent.CallAsync<TResult>(functionName, parameters);

        public static Task CallAsync(string actionName, params object[] parameters) => Agent.CallAsync(actionName, parameters);

        public static async Task WriteToBlankStream<TItem>(PerperStream stream, TItem item, bool keepBinary = false)
        {
            await AsyncLocals.CacheService.StreamWriteItem(stream.Stream, item, keepBinary).ConfigureAwait(false);
        }
    }
}