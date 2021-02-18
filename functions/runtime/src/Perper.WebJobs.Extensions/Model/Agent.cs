using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class Agent : IAgent
    {
        public string AgentName { get; set; }
        public string AgentDelegate { get; set; }

        [NonSerialized] private readonly IContext _context;
        [NonSerialized] private readonly PerperBinarySerializer _serializer;

        [PerperInject]
        protected Agent(IContext context, PerperBinarySerializer serializer)
        {
            _context = context;
            _serializer = serializer;
        }

        public Agent(string agentName, string agentDelegate, IContext context, PerperBinarySerializer serializer)
            : this(context, serializer)
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
            return (TResult)_serializer.DeserializeRoot(callData.Result, typeof(TResult))!;
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            return ((Context)_context).CallAsync(AgentName, AgentDelegate, actionName, parameters);
        }
    }
}