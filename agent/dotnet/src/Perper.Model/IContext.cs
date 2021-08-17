using System.Threading.Tasks;

using Apache.Ignite.Core.Cache.Configuration;

namespace Perper.Model
{
    public interface IContext
    {
        IAgent Agent { get; }

        Task<IAgent> StartAgentAsync(string name, params object[] parameters);
        Task<(IAgent, TResult)> StartAgentAsync<TResult>(string name, params object[] parameters);

        Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null);
        Task<IStream<TItem>> StreamActionAsync<TItem>(string actionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null);
        Task<IStream> StreamActionAsync(string actionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null);

        IStream<TItem> DeclareStreamFunction<TItem>();
        Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, string functionName, object[] parameters, StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null);

        Task<(IStream<TItem>, string)> CreateBlankStreamAsync<TItem>(StreamFlag flags = StreamFlag.Default, QueryEntity queryEntity = null);
    }
}