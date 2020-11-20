using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Cache;

namespace Perper.WebJobs.Extensions.Model
{
    public class Agent : IAgent
    {
        public string AgentName { get; set; }
        public string AgentDelegate { get; set; }

        [NonSerialized] private IIgniteClient _ignite;
        [NonSerialized] private Context _context;

        public Agent(IContext context, IIgniteClient ignite, string agentName, string agentDelegate)
        {
            AgentName = agentName;
            AgentDelegate = agentDelegate;
            _ignite = ignite;
            _context = (Context) context;
        }

        public async Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            var notification = await _context.CallAsync(AgentName, AgentDelegate, functionName, parameters);

            var callsCache = _ignite.GetCache<string, CallData>("calls");
            var resultCall = await callsCache.GetAsync(notification.Call);

            return (TResult)resultCall.Parameters!;
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            return _context.CallAsync(AgentName, AgentDelegate, actionName, parameters);
        }
    }
}