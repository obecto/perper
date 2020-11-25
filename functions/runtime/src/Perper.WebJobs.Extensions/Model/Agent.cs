using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class Agent : IAgent
    {
        public string AgentName { get; set; }
        public string AgentDelegate { get; set; }

        [PerperInject] private IContext _context;
        [PerperInject] private IIgniteClient _ignite;
        [PerperInject] private IServiceProvider _services;

        public Agent(string agentName, string agentDelegate, IContext context, IIgniteClient ignite, IServiceProvider services)
        {
            AgentName = agentName;
            AgentDelegate = agentDelegate;
            _context = context;
            _ignite = ignite;
            _services = services;
        }

        public async Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            var callData = await ((Context)_context).CallAsync(AgentName, AgentDelegate, functionName, parameters);

            if (callData.Result == null)
            {
                // throw new InvalidOperationException($"Called function '{functionName}' did not return a result, did you mean CallActionAsync?");
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