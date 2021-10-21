using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol;

namespace Perper.Extensions
{
    public static class PerperAgentExtensions
    {
        public static async Task<TResult> CallAsync<TResult>(this PerperAgent agent, string functionName, params object?[] parameters)
        {
            var results = await InternalCallAsync(agent, functionName, parameters).ConfigureAwait(false);

            if (results is null)
            {
                return default!;
            }
            else if (typeof(ITuple).IsAssignableFrom(typeof(TResult)))
            {
                return (TResult)Activator.CreateInstance(typeof(TResult), results)!;
            }
            else if (typeof(object[]) == typeof(TResult))
            {
                return (TResult)(object)results;
            }
            else if (results.Length >= 1)
            {
                return (TResult)results[0]!;
            }
            else
            {
                return default!;
            }
        }

        public static async Task CallAsync(this PerperAgent agent, string actionName, params object?[] parameters)
        {
            await InternalCallAsync(agent, actionName, parameters).ConfigureAwait(false);
        }

        private static async Task<object?[]?> InternalCallAsync(PerperAgent agent, string @delegate, object?[] parameters)
        {
            var execution = CacheService.GenerateName(@delegate);

            await AsyncLocals.CacheService.CreateExecution(execution, agent.Agent, agent.Instance, @delegate, parameters).ConfigureAwait(false);

            await AsyncLocals.NotificationService.WaitExecutionFinished(execution).ConfigureAwait(false);

            var results = await AsyncLocals.CacheService.ReadExecutionResult(execution).ConfigureAwait(false);
            await AsyncLocals.CacheService.RemoveExecution(execution).ConfigureAwait(false);

            return results;
        }

        public static async Task DestroyAsync(this PerperAgent agent)
        {
            await AsyncLocals.CacheService.RemoveInstance(agent.Instance).ConfigureAwait(false);
        }
    }
}