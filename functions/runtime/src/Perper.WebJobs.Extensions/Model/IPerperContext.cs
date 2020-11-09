using System;
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

        Task<(IStream<TItem>, string)> StreamExternalAsync<TItem>(StreamFlags flags = StreamFlags.Default);
        Task<IStream<TItem>> StreamFunctionAsync<TItem>(string functionName, object? parameters = default, StreamFlags flags = StreamFlags.Default);
        Task<IStream> StreamActionAsync(string actionName, object? parameters = default, StreamFlags flags = StreamFlags.Default);

        IStream<TItem> DeclareStreamFunction<TItem>(string functionName);
        Task InitializeStreamFunctionAsync<TItem>(IStream<TItem> stream, object? parameters = default);
    }
}