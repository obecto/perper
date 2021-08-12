using System.Threading.Tasks;

namespace Perper.Model
{
    public static class ContextExtensions
    {
        public static Task<TResult> CallFunctionAsync<TResult>(this IContext context, string functionName, object[] parameters)
        {
            return context.Agent.CallFunctionAsync<TResult>(functionName, parameters);
        }

        public static Task CallActionAsync(this IContext context, string actionName, object[] parameters)
        {
            return context.Agent.CallActionAsync(actionName, parameters);
        }
    }
}