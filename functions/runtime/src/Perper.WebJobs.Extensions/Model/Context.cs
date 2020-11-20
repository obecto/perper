using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Apache.Ignite.Core.Client;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Cache.Notifications;

namespace Perper.WebJobs.Extensions.Model
{
    public class Context : IContext
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;

        public string AgentName { get => (TriggerValue["agent"])!.ToString(); }
        public string InstanceName { get => (TriggerValue["stream"] ?? TriggerValue["call"])!.ToString(); }

        public IAgent Agent { get => new Agent(this, _ignite, AgentName, _fabric.AgentDelegate); }
        public JObject TriggerValue { get; set; } = default!;

        public Context(FabricService fabric, IIgniteClient ignite)
        {
            _fabric = fabric;
            _ignite = ignite;
        }

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string delegateName, object? parameters = default)
        {
            var agentsCache = _ignite.GetCache<string, AgentData>("agents");
            var agentName = GenerateName(delegateName);

            var agent = new Agent(this, _ignite, agentName, delegateName);
            await agentsCache.PutAsync(agentName, new AgentData {
                Delegate = delegateName,
            });

            var result = await agent.CallFunctionAsync<TResult>(agentName, parameters);

            return (agent, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName(functionName);
            await CreateStreamAsync(streamName, StreamDelegateType.Function, functionName, parameters, typeof(TItem), flags);
            return new Stream<TItem>() {StreamName = streamName};
        }

        public async Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName(actionName);
            await CreateStreamAsync(streamName, StreamDelegateType.Action, actionName, parameters, null, flags);
            return new Stream() {StreamName = streamName};
        }

        public IStream<TItem> DeclareStreamFunction<TItem>(string functionName)
        {
            var streamName = GenerateName(functionName);
            return new Stream<TItem>() {StreamName = streamName, _functionName = functionName};
        }

        public async Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            await CreateStreamAsync((stream as Stream<TItem>)!.StreamName, StreamDelegateType.Function, (stream as Stream<TItem>)!._functionName!, parameters, null, flags);
        }

        public async Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Ephemeral)
        {
            var streamName = GenerateName();
            await CreateStreamAsync(streamName, StreamDelegateType.External, "", null, typeof(TItem), flags);
            return (new Stream<TItem> {StreamName = streamName}, streamName);
        }

        private async Task CreateStreamAsync(string streamName, StreamDelegateType delegateType, string delegateName, object? parameters, Type? type, StreamFlags flags)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamName, new StreamData {
                AgentDelegate = _fabric.AgentDelegate,
                Delegate = delegateName,
                DelegateType = delegateType,
                Parameters = parameters,
                Listeners = new List<StreamListener>(),
                IndexType = null,
                IndexFields = null,
                Ephemeral = (flags & StreamFlags.Ephemeral) != 0,
                LastModified = DateTime.UtcNow
            });
        }


        public async Task<CallResultNotification> CallAsync(string agentName, string agentDelegate, string callDelegate, object? parameters)
        {

            var callsCache = _ignite.GetCache<string, CallData>("calls");
            var callName = GenerateName(callDelegate);
            await callsCache.PutAsync(callName, new CallData {
                Agent = agentName,
                AgentDelegate = agentDelegate,
                Delegate = callDelegate,
                CallerAgentDelegate = _fabric.AgentDelegate,
                Caller = InstanceName,
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