using System.Collections;
using System.Threading.Tasks;
using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Cache.Standard;

namespace Perper.Model
{
    public class Context : IContext
    {
        public IAgent Agent => new Agent(new PerperAgent(AsyncLocals.Agent, AsyncLocals.Instance));

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult, TParams>(string agent, TParams parameters)
        {
            var instance = new Agent(new PerperAgent(agent, AsyncLocals.CacheService.GenerateName(agent)));
            var result = await instance.CallFunctionAsync<TResult, TParams>(agent, parameters);
            return ((IAgent)instance, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem, TParams>(string @delegate, TParams parameters, StreamFlags flags = StreamFlags.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName(@delegate);
            await CreateStream<TItem, TParams>(stream, @delegate, StreamDelegateType.Function, parameters, flags);
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task<IStream> StreamActionAsync<TParams>(string @delegate, TParams parameters, StreamFlags flags = StreamFlags.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName(@delegate);
            await CreateStream<object?, TParams>(stream, @delegate, StreamDelegateType.Action, parameters, flags);
            return new Stream(new PerperStream(stream));
        }

        public async Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            await CreateStream<TItem, object?>(stream, "", StreamDelegateType.External, null, flags);
            return (new Stream<TItem>(new PerperStream(stream)), stream);
        }

        public IStream<TItem> DeclareStreamFunction<TItem>()
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task InitializeStreamFunctionAsync<TItem, TParams>(IStream<TItem> stream, string @delegate, TParams parameters, StreamFlags flags = StreamFlags.Default)
        {
            await CreateStream<object?, TParams>(((Stream)stream).RawStream.Stream, @delegate, StreamDelegateType.Function, parameters, flags);
        }

        private async Task CreateStream<TItem, TParams>(string stream, string @delegate, StreamDelegateType delegateType, TParams parameters, StreamFlags flags)
        {
            var ephemeral = (flags & StreamFlags.Ephemeral) != 0;

            string? indexType = null;
            Hashtable? indexFields = null;
            if ((flags & StreamFlags.Query) != 0)
            {
                var binaryType = AsyncLocals.CacheService.Ignite.GetBinary().GetBinaryType(typeof(TItem)); // TODO AsyncLocals.Ignite...
                indexType = binaryType.TypeName;
                indexFields = new Hashtable();
                foreach (var field in binaryType.Fields)
                {
                    indexFields[field] = binaryType.GetFieldTypeName(field);
                }
            }

            await AsyncLocals.CacheService.StreamCreate(
                stream, AsyncLocals.Agent, AsyncLocals.Instance, @delegate, delegateType, parameters,
                ephemeral, indexType, indexFields);
        }
    }
}