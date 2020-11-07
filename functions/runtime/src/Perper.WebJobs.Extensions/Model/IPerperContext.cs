using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperContext<out T>
    {
        T Parameters { get; }

        Task FetchStateAsync(object holder);
        Task FetchStateAsync(object holder, string name);
        Task UpdateStateAsync(object holder);
        Task UpdateStateAsync(object holder, string name);

        Task<IAgent> StartAgentAsync(string name, object? parameters = default);

        Task<TResult> CallFunctionAsync<TResult>(string functionName, object? parameters = default);
        Task CallActionAsync(string actionName, object? parameters = default);

        Task<(IStream<TItem>, string)> StreamExternAsync<TItem>(string externName, bool ephemeral = true);
        Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, bool ephemeral = true);
        Task<IStream> StreamActionAsync(string actionName, object? parameters = default, bool ephemeral = true);

        IStream<TItem> DeclareStreamAsync<TItem>(string functionName);
        Task InitializeStreamAsync(IStream stream, object? parameters = default);
    }
}