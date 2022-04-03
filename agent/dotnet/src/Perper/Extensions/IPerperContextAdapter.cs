using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public interface IPerperContextAdapter
    {
        Task<PerperAgent> StartAgentAsync(string agent, params object[] parameters) => AsyncLocalContext.PerperContext.Agents.CreateAsync(agent, parameters);

        Task<(PerperAgent agent, TResult result)> StartAgentAsync<TResult>(string agent, params object[] parameters) => AsyncLocalContext.PerperContext.Agents.CreateAsync<TResult>(agent, parameters);

        Task<TResult> CallAsync<TResult>(string methodName, params object[] parameters) => AsyncLocalContext.PerperContext.CurrentAgent.CallAsync<TResult>(methodName, parameters);

        Task CallAsync(string methodName, params object[] parameters) => AsyncLocalContext.PerperContext.CurrentAgent.CallAsync(methodName, parameters);

        PerperStreamBuilder Stream(string methodName) => new(methodName);
        PerperStreamBuilder BlankStream() => new(null);
    }
}