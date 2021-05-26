using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public static class ContextExtensions
    {
        public static Task<TResult> CallFunctionAsync<TResult>(this IContext context, string functionName, object? parameters = default) => context.Agent.CallFunctionAsync<TResult>(functionName, parameters);

        public static Task CallActionAsync(this IContext context, string actionName, object? parameters = default) => context.Agent.CallActionAsync(actionName, parameters);
    }
}