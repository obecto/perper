using System.Threading.Tasks;

namespace Perper.Model
{
    public static class ContextExtensions
    {
        public static Task<TResult> CallFunctionAsync<TResult, TParams>(this IContext context, string functionName, TParams parameters)
        {
            return context.Agent.CallFunctionAsync<TResult, TParams>(functionName, parameters);
        }

        public static Task CallActionAsync<TParams>(this IContext context, string actionName, TParams parameters)
        {
            return context.Agent.CallActionAsync<TParams>(actionName, parameters);
        }
    }
}