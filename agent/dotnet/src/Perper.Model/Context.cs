using System.Collections;
using System.Threading.Tasks;
using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Cache.Standard;

namespace Perper.Model
{
    public class Context : IContext
    {
        public IAgent Agent => new Agent(new PerperAgent(AsyncLocals.Agent, AsyncLocals.Instance));

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string agent, object[] parameters)
        {
            var instance = new Agent(new PerperAgent(agent, AsyncLocals.CacheService.GenerateName(agent)));
            var result = await instance.CallFunctionAsync<TResult>(agent, parameters);
            return ((IAgent)instance, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string @delegate, object[] parameters, StreamFlags flags = StreamFlags.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName(@delegate);
            await CreateStream<TItem>(stream, @delegate, StreamDelegateType.Function, parameters, flags);
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task<IStream> StreamActionAsync(string @delegate, object[] parameters, StreamFlags flags = StreamFlags.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName(@delegate);
            await CreateStream<object?>(stream, @delegate, StreamDelegateType.Action, parameters, flags);
            return new Stream(new PerperStream(stream));
        }

        public async Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            await CreateStream<TItem>(stream, "", StreamDelegateType.External, null, flags);
            return (new Stream<TItem>(new PerperStream(stream)), stream);
        }

        public IStream<TItem> DeclareStreamFunction<TItem>()
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, string @delegate, object[] parameters, StreamFlags flags = StreamFlags.Default)
        {
            await CreateStream<object?>(((Stream)stream).RawStream.Stream, @delegate, StreamDelegateType.Function, parameters, flags);
        }

        private async Task CreateStream<TItem>(string stream, string @delegate, StreamDelegateType delegateType, object[] parameters, StreamFlags flags)
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