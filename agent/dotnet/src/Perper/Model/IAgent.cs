using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IAgent
    {
        Task<TResult> CallFunctionAsync<TResult>(string functionName, object[] parameters);

        Task CallActionAsync(string actionName, object[] parameters);
    }
}