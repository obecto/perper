using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IContext
    {
        IAgent Agent { get; }

        Task<(IAgent, TResult)> StartAgentAsync<TResult, TParams>(string name, TParams parameters);

        Task<IStream<TItem>> StreamFunctionAsync<TItem, TParams>(string functionName, TParams parameters, StreamFlags flags = StreamFlags.Default);
        Task<IStream> StreamActionAsync<TParams>(string actionName, TParams parameters, StreamFlags flags = StreamFlags.Default);

        IStream<TItem> DeclareStreamFunction<TItem>();
        Task InitializeStreamFunctionAsync<TItem, TParams>(IStream<TItem> stream, string @delegate, TParams parameters, StreamFlags flags = StreamFlags.Default);

        Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default);
    }
}