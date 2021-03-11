using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class Context : IContext
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly PerperInstanceData _instance;
        private readonly PerperBinarySerializer _serializer;
        private readonly IState _state;
        private readonly ILogger<Context> _logger;

        public IAgent Agent => new Agent(_instance.Agent, _fabric.AgentDelegate, this, _serializer);

        public Context(FabricService fabric, PerperInstanceData instance, IIgniteClient ignite, PerperBinarySerializer serializer, IState state, ILogger<Context> logger)
        {
            _fabric = fabric;
            _ignite = ignite;
            _instance = instance;
            _serializer = serializer;
            _state = state;
            _logger = logger;
        }

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string delegateName, object? parameters = default)
        {
            var agentDelegate = delegateName;
            var callDelegate = delegateName;

            var agentName = GenerateName(agentDelegate);
            var agent = new Agent(agentName, agentDelegate, this, _serializer);

            var result = await agent.CallFunctionAsync<TResult>(callDelegate, parameters);

            return (agent, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName(functionName);
            await CreateStreamAsync(streamName, StreamDelegateType.Function, functionName, parameters, typeof(TItem), flags);
            return new Stream<TItem>(streamName, _instance, _fabric, _ignite, _serializer, _state, _logger);
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
            var stream = new Stream<TItem>(streamName, _instance, _fabric, _ignite, _serializer, _state, _logger);
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
            await CreateStreamAsync(streamInstance.StreamName, StreamDelegateType.Function, streamInstance.FunctionName!, parameters, typeof(TItem), flags);
            streamInstance.FunctionName = null;
        }

        public async Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName();
            await CreateStreamAsync(streamName, StreamDelegateType.External, "", null, typeof(TItem), flags);
            return (new Stream<TItem>(streamName, _instance, _fabric, _ignite, _serializer, _state, _logger), streamName);
        }

        private async Task CreateStreamAsync(string streamName, StreamDelegateType delegateType, string delegateName, object? parameters, Type? type, StreamFlags flags)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamName, new StreamData
            {
                Agent = _instance.Agent,
                AgentDelegate = _fabric.AgentDelegate,
                Delegate = delegateName,
                DelegateType = delegateType,
                Parameters = parameters,
                Listeners = new List<StreamListener>(),
                IndexType = (flags & StreamFlags.Query) != 0 && type != null ? PerperTypeUtils.GetJavaTypeName(type) ?? type.Name : null,
                IndexFields = (flags & StreamFlags.Query) != 0 && type != null ? _serializer.GetQueriableFields(type) : null,
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
                Parameters = parameters,
            });

            var (key, notification) = await _fabric.GetCallNotification(callName);
            await _fabric.ConsumeNotification(key);

            var callResult = await callsCache.GetAndRemoveAsync(notification.Call);
            var call = callResult.Value;

            if (call.Error != null)
            {
                throw new Exception("Exception in call to '" + callDelegate + "': " + call.Error);
            }
            else
            {
                return call;
            }
        }

        private string GenerateName(string? baseName = null)
        {
            return $"{baseName}-{Guid.NewGuid()}";
        }
    }
}