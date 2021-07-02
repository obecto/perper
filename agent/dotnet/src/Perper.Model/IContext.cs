using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IContext
    {
        IAgent Agent { get; }

        Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, object[] parameters);

        Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default);
        Task<IStream> StreamActionAsync(string actionName, object[] parameters, StreamFlag flags = StreamFlag.Default);

        IStream<TItem> DeclareStreamFunction<TItem>();
        Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default);

        Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlag flags = StreamFlag.Default);
    }
}