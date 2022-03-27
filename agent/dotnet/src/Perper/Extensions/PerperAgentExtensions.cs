using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol;

namespace Perper.Extensions
{
    public static class PerperAgentExtensions
    {
        public static async Task<TResult> CallAsync<TResult>(this PerperAgent agent, string functionName, params object?[] parameters)
        {
            var results = await InternalCallAsync(agent, functionName, parameters, default).ConfigureAwait(false);

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
            await InternalCallAsync(agent, actionName, parameters, default).ConfigureAwait(false);
        }

        private static async Task<object?[]?> InternalCallAsync(PerperAgent agent, string @delegate, object?[] parameters, CancellationToken cancellationToken)
        {
            var execution = FabricService.GenerateName(@delegate);

            await AsyncLocals.FabricService.CreateExecution(execution, agent.Agent, agent.Instance, @delegate, parameters).ConfigureAwait(false);

            try
            {
                await AsyncLocals.FabricService.WaitExecutionFinished(execution, cancellationToken).ConfigureAwait(false);

                var results = await AsyncLocals.FabricService.ReadExecutionResult(execution).ConfigureAwait(false);
                await AsyncLocals.FabricService.RemoveExecution(execution).ConfigureAwait(false);

                return results;
            }
            catch (OperationCanceledException)
            {
                await AsyncLocals.FabricService.RemoveExecution(execution).ConfigureAwait(false);
                throw;
            }

        }

        public static async Task DestroyAsync(this PerperAgent agent)
        {
            await foreach(var child in agent.Children)
            {
                await child.Value.DestroyAsync().ConfigureAwait(false);
            }

            await AsyncLocals.FabricService.RemoveInstance(agent.Instance).ConfigureAwait(false);
        }
    }
}