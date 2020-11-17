using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Cache.Notifications;

namespace Perper.WebJobs.Extensions.Model
{
    public class Agent : IAgent
    {
        public string AgentName { get; set; }
        public string AgentDelegate { get; set; }

        [NonSerialized] private FabricService _fabric;
        [NonSerialized] private IIgniteClient _ignite;
        [NonSerialized] private string _caller;

        public async Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            var notification = await CallAsync(functionName, parameters);

            var callsCache = _ignite.GetCache<string, CallData>("calls");
            var resultCall = await callsCache.GetAsync(notification.Call);

            return (TResult)resultCall.Parameters!;
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            return CallAsync(actionName, parameters);
        }

        private async Task<CallResultNotification> CallAsync(string callDelegate, object? parameters)
        {
            var callsCache = _ignite.GetCache<string, CallData>("calls");
            var callName = GenerateName(callDelegate);
            await callsCache.PutAsync(callName, new CallData {
                Agent = AgentName,
                AgentDelegate = AgentDelegate,
                Delegate = callDelegate,
                CallerAgentDelegate = _fabric.AgentDelegate,
                Caller = _caller,
                Finished = false,
                LocalToData = true,
                Parameters = parameters,
            });

            var (key, notification) = await _fabric.GetCallNotification(callName);
            await _fabric.ConsumeNotification(key);

            return (notification as CallResultNotification)!;
        }

        private string GenerateName(string? baseName = null)
        {
            return $"{baseName}-{Guid.NewGuid()}";
        }
    }
}