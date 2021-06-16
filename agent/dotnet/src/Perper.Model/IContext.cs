using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IContext
    {
        IAgent Agent { get; }

        Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, object? parameters = default);

        Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default);
        Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamFlags flags = StreamFlags.Default);

        IStream<TItem> DeclareStreamFunction<TItem>(string functionName);
        Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default, StreamFlags flags = StreamFlags.Default);

        Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default);
    }
}