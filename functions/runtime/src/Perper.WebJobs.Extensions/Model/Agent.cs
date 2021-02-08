using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class Agent : IAgent
    {
        public string AgentName { get; set; }
        public string AgentDelegate { get; set; }

        [NonSerialized] private IContext _context;

        [PerperInject]
        protected Agent(IContext context)
        {
            _context = context;
        }

        public Agent(string agentName, string agentDelegate, IContext context)
            : this(context)
        {
            AgentName = agentName;
            AgentDelegate = agentDelegate;
        }

        public async Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            var callData = await ((Context)_context).CallAsync(AgentName, AgentDelegate, functionName, parameters);

            if (callData.Result == null)
            {
                return default(TResult)!;
            }

            return (TResult)callData.Result;
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            return ((Context)_context).CallAsync(AgentName, AgentDelegate, actionName, parameters);
        }
    }
}