using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IAgent
    {
        Task<T> CallFunctionAsync<T>(string functionName, object? parameters = default);
        Task CallActionAsync(string actionName, object? parameters = default);
    }
}