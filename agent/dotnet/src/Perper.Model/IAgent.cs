using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IAgent
    {
        Task<TResult> CallFunctionAsync<TResult, TParams>(string functionName, TParams parameters);
        Task CallActionAsync(string actionName, object? parameters = default);
    }
}