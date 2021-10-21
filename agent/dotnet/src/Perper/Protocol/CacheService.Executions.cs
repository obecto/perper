using System.Threading.Tasks;

using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public partial class CacheService
    {
        public Task CreateExecution(string execution, string agent, string instance, string @delegate, object?[] parameters)
        {
            var executionData = new ExecutionData(agent, instance, @delegate, parameters);

            return executionsCache.PutIfAbsentOrThrowAsync(execution, executionData);
        }

        public Task RemoveExecution(string execution)
        {
            return executionsCache.RemoveAsync(execution);
        }

        public Task WriteExecutionFinished(string execution)
        {
            return executionsCache.OptimisticUpdateAsync(execution, igniteBinary, value => { value.Finished = true; });
        }

        public Task WriteExecutionResult(string execution, object?[] result)
        {
            return executionsCache.OptimisticUpdateAsync(execution, igniteBinary, value => { value.Finished = true; value.Result = result; });
        }

        public Task WriteExecutionError(string execution, string error)
        {
            return executionsCache.OptimisticUpdateAsync(execution, igniteBinary, value => { value.Finished = true; value.Error = error; });
        }

        public async Task<object?[]> ReadExecutionParameters(string execution)
        {
            return (await executionsCache.GetAsync(execution).ConfigureAwait(false)).Parameters;
        }

        public async Task<string?> ReadExecutionError(string execution)
        {
            return (await executionsCache.GetAsync(execution).ConfigureAwait(false)).Error;
        }

        public async Task<(string?, object?[]?)> ReadExecutionErrorAndResult(string execution)
        {
            var executionData = await executionsCache.GetAsync(execution).ConfigureAwait(false);

            return (executionData.Error, executionData.Result);
        }
    }
}