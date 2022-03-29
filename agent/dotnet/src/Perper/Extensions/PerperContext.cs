using System;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public static class PerperContext
    {
        public static PerperAgent Agent => AsyncLocalContext.PerperContext.CurrentAgent;

        [Obsolete("Moved to Perper.Model")]
        public static string StartupFunctionName => PerperAgentsExtensions.StartupFunctionName;

        public static Task<PerperAgent> StartAgentAsync(string agent, params object[] parameters) =>
            AsyncLocalContext.PerperContext.Agents.CreateAsync(agent, parameters);

        public static Task<(PerperAgent agent, TResult result)> StartAgentAsync<TResult>(string agent, params object[] parameters) =>
            AsyncLocalContext.PerperContext.Agents.CreateAsync<TResult>(agent, parameters);

        public static Task<TResult> CallAsync<TResult>(string @delegate, params object[] parameters) =>
            Agent.CallAsync<TResult>(@delegate, parameters);

        public static Task CallAsync(string @delegate, params object[] parameters) =>
            Agent.CallAsync(@delegate, parameters);

        public static PerperStreamBuilder Stream(string @delegate) => new(@delegate);
        public static PerperStreamBuilder BlankStream() => new(null);
    }
}