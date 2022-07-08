using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;

using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public async Task CreateExecution(string execution, string agent, string instance, string @delegate, object?[] parameters)
        {
            var executionData = new ExecutionData(agent, instance, @delegate, parameters);

            await ExecutionsCache.PutIfAbsentOrThrowAsync(execution, executionData).ConfigureAwait(false);
        }

        public async Task RemoveExecution(string execution)
        {
            await ExecutionsCache.RemoveAsync(execution).ConfigureAwait(false);
        }

        public async Task WriteExecutionFinished(string execution)
        {
            await ExecutionsCache.OptimisticUpdateAsync(execution, IgniteBinary, value => { value.Finished = true; }).ConfigureAwait(false);
        }

        public async Task WriteExecutionResult(string execution, object?[] result)
        {
            // NOTE: Using builder directly to allow for binary objects to be passed and directly unwrapped in the result; otherwise, Ignite will not unwrap them on the receiving side.
            // await ExecutionsCache.OptimisticUpdateAsync(execution, IgniteBinary, value => { value.Finished = true; value.Result = result; }).ConfigureAwait(false);
            await ExecutionsCache.WithKeepBinary<string, IBinaryObject>().OptimisticUpdateAsync(execution, (binaryObject) =>
            {
                var builder = binaryObject.ToBuilder();
                builder.SetField("Finished", true);
                builder.SetField("Result", result);
                return builder.Build();
            }).ConfigureAwait(false);
        }

        public async Task WriteExecutionError(string execution, string error)
        {
            await ExecutionsCache.OptimisticUpdateAsync(execution, IgniteBinary, value => { value.Finished = true; value.Error = error; }).ConfigureAwait(false);
        }

        public async Task<object?[]> ReadExecutionParameters(string execution)
        {
            return (await ExecutionsCache.GetAsync(execution).ConfigureAwait(false)).Parameters;
        }

        public async Task<string?> ReadExecutionError(string execution)
        {
            return (await ExecutionsCache.GetAsync(execution).ConfigureAwait(false)).Error;
        }

        public async Task<(string?, object?[]?)> ReadExecutionErrorAndResult(string execution)
        {
            var executionData = await ExecutionsCache.GetAsync(execution).ConfigureAwait(false);

            return (executionData.Error, executionData.Result);
        }
    }
}