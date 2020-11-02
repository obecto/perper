using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Streams
{
    public interface IAgent
    {
        Task<T> CallFunctionAsync<T>(string functionName, params object[] parameters);
        Task CallActionAsync(string actionName, params object[] parameters);
    }
}