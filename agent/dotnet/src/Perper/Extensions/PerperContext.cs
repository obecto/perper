using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol;

namespace Perper.Extensions
{
    public static class PerperContext
    {
        public static string StartupFunctionName { get; } = "Start";
        public static string StopFunctionName { get; } = "Stop";

        // Note: Deprecated - exists just for backwards compatibility with old `Startup`. Will be removed soon.
        // Projects should switch to using `Start` instead.
        public static string FallbackStartupFunctionName { get; } = "Startup";

        public static PerperAgent Agent => new(AsyncLocals.Agent, AsyncLocals.Instance);

        public static async Task<PerperAgent> StartAgentAsync(string agent, params object[] parameters)
        {
            var instance = await CreateInstanceAsync(agent).ConfigureAwait(false);
            await instance.CallAsync(StartupFunctionName, parameters).ConfigureAwait(false);
            await instance.CallAsync(FallbackStartupFunctionName, parameters).ConfigureAwait(false);
            return instance;
        }

        public static async Task<(PerperAgent, TResult)> StartAgentAsync<TResult>(string agent, params object[] parameters)
        {
            var instance = await CreateInstanceAsync(agent).ConfigureAwait(false);
            var result = await instance.CallAsync<TResult>(StartupFunctionName, parameters).ConfigureAwait(false) ??
                         await instance.CallAsync<TResult>(FallbackStartupFunctionName, parameters).ConfigureAwait(false);
            return (instance, result);
        }

        private static async Task<PerperAgent> CreateInstanceAsync(string agent)
        {
            var instance = FabricService.GenerateName(agent);
            await AsyncLocals.FabricService.CreateInstance(instance, agent).ConfigureAwait(false);
            var resultAgent = new PerperAgent(agent, instance);

            await Agent.GetChildren()
                .AddAsync(resultAgent.Instance, resultAgent.Agent)
                .ConfigureAwait(false);

            return resultAgent;
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

        public static async Task WriteToBlankStreamAsync<TItem>(PerperStream stream, long key, TItem item, bool keepBinary = false)
        {
            await AsyncLocals.FabricService.WriteStreamItem(stream.Stream, key, item, keepBinary).ConfigureAwait(false);
        }

        public static async Task WriteToBlankStreamAsync<TItem>(PerperStream stream, TItem item, bool keepBinary = false)
        {
            await AsyncLocals.FabricService.WriteStreamItem(stream.Stream, item, keepBinary).ConfigureAwait(false);
        }
    }
}