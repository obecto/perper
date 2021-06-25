using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IContext
    {
        IAgent Agent { get; }

        Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, object[] parameters);

        Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object[] parameters, StreamFlags flags = StreamFlags.Default);
        Task<IStream> StreamActionAsync(string actionName, object[] parameters, StreamFlags flags = StreamFlags.Default);

        IStream<TItem> DeclareStreamFunction<TItem>();
        Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, string @delegate, object[] parameters, StreamFlags flags = StreamFlags.Default);

        Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlags flags = StreamFlags.Default);
    }
}