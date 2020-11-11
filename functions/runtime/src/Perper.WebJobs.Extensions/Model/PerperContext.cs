using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Binary;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Cache.Notifications;

namespace Perper.WebJobs.Extensions.Model
{
    public class PerperContext<T>
    {
        public T Parameters { get; }

        [NonSerialized] private FabricService _fabric;
        [NonSerialized] private IIgniteClient _ignite;
        [NonSerialized] private Agent _self;

        /*public PerperContext(StreamData stream)
        {
            _self = new Agent { AgentName = stream.Agent, AgentDelegate = stream.AgentDelegate };
            Parameters = (T) stream.Parameters ?? default(T)!;
        }*/

        public Task FetchStateAsync(object holder) => Task.CompletedTask;
        public Task FetchStateAsync(object holder, string name) => Task.CompletedTask;
        public Task UpdateStateAsync(object holder) => Task.CompletedTask;
        public Task UpdateStateAsync(object holder, string name) => Task.CompletedTask;

        public async Task<IAgent> StartAgentAsync(string delegateName, object? parameters = default)
        {
            var agentsCache = _ignite.GetCache<string, AgentData>("agents");
            var agentName = GenerateName(delegateName);
            await agentsCache.PutAsync(agentName, new AgentData {
                Delegate = delegateName,
            });

            return default!;
        }

        public Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default)
        {
            return _self.CallFunctionAsync<TResult>(functionName, parameters);
        }

        public Task CallActionAsync(string actionName, object? parameters = default)
        {
            return _self.CallActionAsync(actionName, parameters);
        }

        public async Task<(IStream<TItem>, string)> StreamExternalAsync<TItem>(StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName();
            await StartStreamAsync(streamName, StreamDelegateType.External, "", null, typeof(TItem), flags);
            return (new Stream<TItem>() {StreamName = streamName}, streamName);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName(functionName);
            await StartStreamAsync(streamName, StreamDelegateType.Function, functionName, parameters, typeof(TItem), flags);
            return new Stream<TItem>() {StreamName = streamName};
        }

        public async Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var streamName = GenerateName(actionName);
            await StartStreamAsync(streamName, StreamDelegateType.Action, actionName, parameters, null, flags);
            return new Stream() {StreamName = streamName};
        }

        public IStream<TItem> DeclareStreamFunction<TItem>(string functionName)
        {
            var streamName = GenerateName(functionName);
            return new Stream<TItem>() {StreamName = streamName};
        }

        public async Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default, StreamFlags flags = StreamFlags.Default)
        {
            var functionName = default(string)!;
            await StartStreamAsync((stream as Stream<TItem>)!.StreamName, StreamDelegateType.Action, functionName, parameters, null, flags);
        }

        private async Task StartStreamAsync(string streamName, StreamDelegateType delegateType, string delegateName, object? parameters, Type? type, StreamFlags flags)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamName, new StreamData {
                AgentDelegate = _self.AgentDelegate,
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

        private string GenerateName(string? baseName = null)
        {
            return $"{baseName}-{Guid.NewGuid()}";
        }
    }
}