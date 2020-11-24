using System;
using System.Runtime.CompilerServices;
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

        public string AgentName { get; private set; } = default!;
        public string InstanceName { get; private set; } = default!;

        public IAgent Agent { get => new Agent(this, _ignite, AgentName, _fabric.AgentDelegate); }

        public Context(FabricService fabric, IIgniteClient ignite)
        {
            _fabric = fabric;
            _ignite = ignite;
        }

        public async Task SetTriggerValue(JObject triggerValue)
        {
            if (triggerValue.ContainsKey("Call"))
            {
                InstanceName = (string) triggerValue["Call"]!;
                var callsCache = _ignite.GetCache<string, CallData>("calls");
                var callData = await callsCache.GetAsync(InstanceName);
                AgentName = callData.Agent!;
            }
            else
            {
                InstanceName = (string) triggerValue["Stream"]!;
                var streamsCache = _ignite.GetCache<string, StreamData>("streams");
                var streamData = await streamsCache.GetAsync(InstanceName);
                AgentName = streamData.Agent!;
            }
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
                Parameters = ConvertParameters(parameters),
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
                Parameters = ConvertParameters(parameters),
            });

            var (key, notification) = await _fabric.GetCallNotification(callName);
            await _fabric.ConsumeNotification(key);

            return (notification as CallResultNotification)!;
        }

        private object?[] ConvertParameters(object? parameters)
        {
            if (parameters is ITuple tuple)
            {
                var result = new object?[tuple.Length];
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = tuple[i];
                }
                return result;
            }
            else if (parameters is object?[] array)
            {
                return array;
            }
            else
            {
                // Shouldn't happen
                return new object?[] {parameters};
            }
        }


        private string GenerateName(string? baseName = null)
        {
            return $"{baseName}-{Guid.NewGuid()}";
        }
    }
}