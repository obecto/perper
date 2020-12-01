using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class Context : IContext
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly PerperInstanceData _instance;
        private readonly IState _state;

        public IAgent Agent => new Agent(_instance.InstanceData.Agent, _fabric.AgentDelegate, this, _ignite);

        public Context(FabricService fabric, PerperInstanceData instance, IIgniteClient ignite, IState state)
        {
            _fabric = fabric;
            _ignite = ignite;
            _instance = instance;
            _state = state;
        }

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string delegateName, object? parameters = default)
        {
            var agentsCache = _ignite.GetCache<string, AgentData>("agents");
            var agentName = GenerateName(delegateName);

            var agent = new Agent(agentName, delegateName, this, _ignite);
            await agentsCache.PutAsync(agentName, new AgentData
            {
                Delegate = delegateName,
            });

            var result = await agent.CallFunctionAsync<TResult>(agentName, parameters);

            return (agent, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName(functionName);
            await CreateStreamAsync(streamName, StreamDelegateType.Function, functionName, parameters, typeof(TItem), flags);
            return new Stream<TItem>(streamName, _instance, _fabric, _ignite, _state);
        }

        public async Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName(actionName);
            await CreateStreamAsync(streamName, StreamDelegateType.Action, actionName, parameters, null, flags);
            return new Stream(streamName, _fabric, _ignite);
        }

        public IStream<TItem> DeclareStreamFunction<TItem>(string functionName)
        {
            var streamName = GenerateName(functionName);
            var stream = new Stream<TItem>(streamName, _instance, _fabric, _ignite, _state);
            stream.FunctionName = functionName;
            return stream;
        }

        public async Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamInstance = (Stream<TItem>)stream;
            if (streamInstance.FunctionName == null)
            {
                throw new InvalidOperationException("Stream is already initialized");
            }
            await CreateStreamAsync(streamInstance.StreamName, StreamDelegateType.Function, streamInstance.FunctionName!, parameters, null, flags);
            streamInstance.FunctionName = null;
        }

        public async Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Ephemeral)
        {
            var streamName = GenerateName();
            await CreateStreamAsync(streamName, StreamDelegateType.External, "", null, typeof(TItem), flags);
            return (new Stream<TItem>(streamName, _instance, _fabric, _ignite, _state), streamName);
        }

        private async Task CreateStreamAsync(string streamName, StreamDelegateType delegateType, string delegateName, object? parameters, Type? type, StreamFlags flags)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamName, new StreamData
            {
                Agent = _instance.InstanceData.Agent,
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

        public async Task<CallData> CallAsync(string agentName, string agentDelegate, string callDelegate, object? parameters)
        {
            var callsCache = _ignite.GetCache<string, CallData>("calls");
            var callName = GenerateName(callDelegate);
            await callsCache.PutAsync(callName, new CallData
            {
                Agent = agentName,
                AgentDelegate = agentDelegate,
                Delegate = callDelegate,
                CallerAgentDelegate = _fabric.AgentDelegate,
                Caller = _instance.InstanceName,
                Finished = false,
                LocalToData = true,
                Parameters = ConvertParameters(parameters),
            });

            var (key, notification) = await _fabric.GetCallNotification(callName);
            await _fabric.ConsumeNotification(key);

            var callResult = await callsCache.GetAndRemoveAsync(notification.Call);
            var call = callResult.Value;

            if (call.Error != null)
            {
                throw new Exception("Exception in call: " + call.Error);
            }
            else
            {
                return call;
            }
        }

        private object?[] ConvertParameters(object? parameters)
        {
#if !NETSTANDARD2_0
            if (parameters is ITuple tuple)
            {
                var result = new object?[tuple.Length];
                for (var i = 0; i < result.Length; i++)
                {
                    result[i] = tuple[i];
                }
                return result;
            }
#endif
            if (parameters is object?[] array)
            {
                return array;
            }

            return new object?[] { parameters };
        }

        private string GenerateName(string? baseName = null)
        {
            return $"{baseName}-{Guid.NewGuid()}";
        }
    }
}