using System.Collections;
using System.Threading.Tasks;

using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Cache.Standard;

namespace Perper.Model
{
    public class Context : IContext
    {
        public static string StartupFunctionName { get; } = "Startup";

        public IAgent Agent => new Agent(new PerperAgent(AsyncLocals.Agent, AsyncLocals.Instance));

        public async Task<IAgent> StartAgentAsync(string name, params object[] parameters)
        {
            var instance = new Agent(new PerperAgent(name, AsyncLocals.CacheService.GenerateName(name)));
            await instance.CallActionAsync(StartupFunctionName, parameters).ConfigureAwait(false);
            return instance;
        }

        public async Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, params object[] parameters)
        {
            var instance = new Agent(new PerperAgent(name, AsyncLocals.CacheService.GenerateName(name)));
            var result = await instance.CallFunctionAsync<TResult>(StartupFunctionName, parameters).ConfigureAwait(false);
            return (instance, result);
        }

        public async Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName(functionName);
            await CreateStream<TItem>(stream, functionName, StreamDelegateType.Function, parameters, flags).ConfigureAwait(false);
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task<IStream> StreamActionAsync(string actionName, object[] parameters, StreamFlag flags = StreamFlag.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName(actionName);
            await CreateStream<object?>(stream, actionName, StreamDelegateType.Action, parameters, flags).ConfigureAwait(false);
            return new Stream(new PerperStream(stream));
        }

        public async Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlag flags = StreamFlag.Default)
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            await CreateStream<TItem>(stream, "", StreamDelegateType.External, null, flags).ConfigureAwait(false);
            return (new Stream<TItem>(new PerperStream(stream)), stream);
        }

        public IStream<TItem> DeclareStreamFunction<TItem>()
        {
            var stream = AsyncLocals.CacheService.GenerateName("");
            return new Stream<TItem>(new PerperStream(stream));
        }

        public async Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default)
        {
            await CreateStream<object?>(((Stream)stream).RawStream.Stream, functionName, StreamDelegateType.Function, parameters, flags).ConfigureAwait(false);
        }

        private async Task CreateStream<TItem>(string stream, string @delegate, StreamDelegateType delegateType, object[] parameters, StreamFlag flags)
        {
            var ephemeral = (flags & StreamFlag.Ephemeral) != 0;

            string? indexType = null;
            Hashtable? indexFields = null;
            if ((flags & StreamFlag.Query) != 0)
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
                ephemeral, indexType, indexFields).ConfigureAwait(false);
        }
    }
}