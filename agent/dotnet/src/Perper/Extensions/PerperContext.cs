using System;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public static class PerperContext
    {
        public static IPerperContext Perper => AsyncLocalContext.PerperContext;
        public static PerperInstance Agent => AsyncLocalContext.PerperContext.CurrentAgent;

        [Obsolete("Moved to Perper.Model.PerperAgentsExtensions.StartFunctionName")]
        public static string StartupFunctionName => PerperAgentsExtensions.StartFunctionName;

        public static Task<PerperInstance> StartAgentAsync(string agent, params object[] parameters) =>
            AsyncLocalContext.PerperContext.CreateAgentAsync(agent, parameters);

        public static Task<(PerperInstance Instance, TResult Result)> StartAgentAsync<TResult>(string agent, params object[] parameters) =>
            AsyncLocalContext.PerperContext.CreateAgentAsync<TResult>(agent, parameters);

        public static Task<TResult> CallAsync<TResult>(string @delegate, params object[] parameters) => Agent.CallAsync<TResult>(@delegate, parameters);

        public static Task CallAsync(string @delegate, params object[] parameters) => Agent.CallAsync(@delegate, parameters);

        public static PerperStreamBuilder Stream(string @delegate) => new(@delegate);
        public static PerperStreamBuilder BlankStream() => new(null);
    }
}