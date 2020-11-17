using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public static class ContextExtensions
    {
        public static Task<TResult> CallFunctionAsync<TResult>(this IContext context, string functionName, object? parameters = default)
        {
            return context.Agent.CallFunctionAsync<TResult>(functionName, parameters);
        }

        public static Task CallActionAsync(this IContext context, string actionName, object? parameters = default)
        {
            return context.Agent.CallActionAsync(actionName, parameters);
        }
    }
}