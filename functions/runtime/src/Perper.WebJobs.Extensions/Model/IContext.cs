using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IContext
    {
        IAgent Agent { get; }

        Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, object? parameters = default);

        Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamOptions flags = StreamOptions.Default);
        Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamOptions flags = StreamOptions.Default);

        IStream<TItem> DeclareStreamFunction<TItem>(string functionName);
        Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default, StreamOptions flags = StreamOptions.Default);

        Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamOptions flags = StreamOptions.Default);
    }
}