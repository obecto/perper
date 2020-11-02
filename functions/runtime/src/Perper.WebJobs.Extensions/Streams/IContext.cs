using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Streams
{
    public interface IContext
    {
        Task FetchStateAsync(object holder);
        Task UpdateStateAsync(object holder);
        Task UpdateStateAsync<T>(string name, T state);
        Task UpdateStateAsync<TKey, TVal>(string name, TKey key, TVal value);

        Task<IAgent> StartAgentAsync(string name, params object[] parameters);

        Task<T> CallFunctionAsync<T>(string functionName, params object[] parameters);
        Task CallActionAsync(string actionName, params object[] parameters);

        Task<IStream<T>> StreamFunctionAsync<T>(string functionName, params object[] parameters);
        Task<IStream> StreamActionAsync(string actionName, params object[] parameters);
        Task UpdateStreamAsync(IStream stream, params object[] parameters);
    }
}