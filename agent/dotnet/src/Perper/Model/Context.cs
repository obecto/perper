using System;
using System.Collections;
using System.Threading.Tasks;

using Apache.Ignite.Core.Cache.Configuration;

using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Cache.Standard;

namespace Perper.Model
{
    public class Context : IContext
    {
        public static string StartupFunctionName { get; } = "Startup";

        public IAgent Agent => new Agent(new PerperAgent(AsyncLocals.Agent, AsyncLocals.Instance));

        public async Task<IAgent> StartAgentAsync(string agent, params object[] parameters)
        {
            var instance = AsyncLocals.CacheService.GenerateName(agent);
            await AsyncLocals.CacheService.InstanceCreate(instance, agent).ConfigureAwait(false);
            var model = new Agent(new PerperAgent(agent, instance));
            await model.CallActionAsync(StartupFunctionName, parameters).ConfigureAwait(false);
            return model;
        }

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string agent, params object[] parameters)
        {
            var instance = AsyncLocals.CacheService.GenerateName(agent);
            await AsyncLocals.CacheService.InstanceCreate(instance, agent).ConfigureAwait(false);
            var model = new Agent(new PerperAgent(agent, instance));
            var result = await model.CallFunctionAsync<TResult>(StartupFunctionName, parameters).ConfigureAwait(false);
            return (model, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null)
        {
            var stream = AsyncLocals.CacheService.GenerateName(functionName);
            await CreateStream(stream, functionName, StreamDelegateType.Function, parameters, () => queryEntity ?? GetQueryEntity<TItem>(), flags).ConfigureAwait(false);
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task<IStream<TItem>> StreamActionAsync<TItem>(string actionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null)
        {
            // FIXME: Move Function/Action distinction to StreamFlag
            var stream = AsyncLocals.CacheService.GenerateName(actionName);
            await CreateStream(stream, actionName, StreamDelegateType.Action, parameters, () => queryEntity ?? GetQueryEntity<TItem>(), flags).ConfigureAwait(false);
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task<IStream> StreamActionAsync(string actionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null)
        {
            var stream = AsyncLocals.CacheService.GenerateName(actionName);
            await CreateStream(stream, actionName, StreamDelegateType.Action, parameters, () => queryEntity ?? GetQueryEntity<object?>(), flags).ConfigureAwait(false);
            return new Stream(new PerperStream(stream));
        }

        public async Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null)
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            await CreateStream(stream, "", StreamDelegateType.External, null, () => queryEntity ?? GetQueryEntity<TItem>(), flags).ConfigureAwait(false);
            return (new Stream<TItem>(new PerperStream(stream)), stream);
        }

        public IStream<TItem> DeclareStreamFunction<TItem>()
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null)
        {
            await CreateStream(((Stream)stream).RawStream.Stream, functionName, StreamDelegateType.Function, parameters, () => queryEntity ?? GetQueryEntity<TItem>(), flags).ConfigureAwait(false);
        }

        private QueryEntity GetQueryEntity<T>()
        {
            return new QueryEntity(typeof(T));
        }

        private async Task CreateStream(string stream, string @delegate, StreamDelegateType delegateType, object[] parameters, Func<QueryEntity> getQueryEntity, StreamFlag flags)
        {
            var ephemeral = (flags & StreamFlag.Ephemeral) != 0;

            string? indexType = null;
            Hashtable? indexFields = null;
            if ((flags & StreamFlag.Query) != 0)
            {
                var queryEntity = getQueryEntity();
                indexType = queryEntity.ValueTypeName;
                if (!(indexType is null))
                {
                    indexType = indexType[(indexType.LastIndexOf(".") + 1)..]; // Workaround bug with QueryEntity
                }
                indexFields = new Hashtable();
                if (queryEntity.Fields != null)
                {
                    foreach (var field in queryEntity.Fields)
                    {
                        indexFields[field.Name] = field.FieldTypeName;
                    }
                }
                // Else: Log warning that there are no indexed fields
            }

            await AsyncLocals.CacheService.StreamCreate(
                stream, AsyncLocals.Agent, AsyncLocals.Instance, @delegate, delegateType, parameters,
                ephemeral, indexType, indexFields).ConfigureAwait(false);
        }
    }
}