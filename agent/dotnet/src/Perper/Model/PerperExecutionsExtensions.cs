using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperExecutionsExtensions
    {
        public static async Task<PerperExecution> CreateAsync(this IPerperExecutions perperExecutions, PerperAgent agent, string @delegate, params object?[] arguments)
        {
            var (execution, delayedCreate) = perperExecutions.Create(agent, @delegate);
            await delayedCreate(arguments).ConfigureAwait(false);
            return execution;
        }

        public static async Task<TResult> CallAsync<TResult>(this IPerperExecutions executions, PerperAgent agent, string @delegate, params object?[] arguments)
        {
            var execution = await executions.CreateAsync(agent, @delegate, arguments).ConfigureAwait(false);
            var result = await executions.GetResultAsync<TResult>(execution).ConfigureAwait(false);
            await executions.DestroyAsync(execution).ConfigureAwait(false);
            return result;
        }

        public static async Task CallAsync(this IPerperExecutions executions, PerperAgent agent, string @delegate, params object?[] arguments)
        {
            var execution = await executions.CreateAsync(agent, @delegate, arguments).ConfigureAwait(false);
            await executions.GetResultAsync(execution).ConfigureAwait(false);
            await executions.DestroyAsync(execution).ConfigureAwait(false);
        }

        public static Task<TResult> CallAsync<TResult>(this IPerper perper, PerperAgent agent, string @delegate, params object?[] arguments) =>
            perper.Executions.CallAsync<TResult>(agent, @delegate, arguments);

        public static Task CallAsync(this IPerper perper, PerperAgent agent, string @delegate, params object?[] arguments) =>
            perper.Executions.CallAsync(agent, @delegate, arguments);

        public static Task<TResult> CallAsync<TResult>(this IPerperContext perper, string @delegate, params object?[] arguments) =>
            perper.CallAsync<TResult>(perper.CurrentAgent, @delegate, arguments);

        public static Task CallAsync(this IPerperContext perper, string @delegate, params object?[] arguments) =>
            perper.CallAsync(perper.CurrentAgent, @delegate, arguments);

        public static (PerperExecution Execution, DelayedCreateFunc Start) CreateExecution(this IPerperContext perper, string @delegate) =>
            perper.Executions.Create(perper.CurrentAgent, @delegate);

        public static Task<PerperExecution> CreateExecutionAsync(this IPerperContext perper, string @delegate, params object?[] arguments) =>
            perper.Executions.CreateAsync(perper.CurrentAgent, @delegate, arguments);

        public static Task DestroyExecutionAsync(this IPerper perper, PerperExecution execution) =>
            perper.Executions.DestroyAsync(execution);

        private static readonly MethodInfo WriteResultMethod = typeof(IPerperExecutions).GetMethods().Single(x => x.Name == "WriteResultAsync" && x.IsGenericMethod);
        public static Task WriteResultAsync(this IPerperExecutions perperExecutions, PerperExecution execution, Type resultType, object? result)
        {
            if (resultType == typeof(void))
            {
                return perperExecutions.WriteResultAsync(execution);
            }
            else
            {
                return (Task)WriteResultMethod.MakeGenericMethod(new[] { resultType }).Invoke(perperExecutions, new[] { execution, result })!;
            }
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        public static async Task WriteTaskAsync<TResult>(this IPerperExecutions perperExecutions, PerperExecution execution, Task<TResult> task)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                await perperExecutions.WriteResultAsync(execution, result).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await perperExecutions.WriteExceptionAsync(execution, exception).ConfigureAwait(false);
            }
        }
    }
}