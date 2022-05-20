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

        public async Task WriteExecutionResult(string execution, object?[] result, bool keepBinary = false)
        {
            if (!keepBinary)
            {
                await ExecutionsCache.OptimisticUpdateAsync(execution, IgniteBinary, value => { value.Finished = true; value.Result = result; }).ConfigureAwait(false);
            }
            else
            {
                await ExecutionsCache.WithKeepBinary<string, IBinaryObject>().OptimisticUpdateAsync(execution, value =>
                {
                    var builder = value.ToBuilder();
                    builder.SetField(nameof(ExecutionData.Finished), true);
                    builder.SetField(nameof(ExecutionData.Result), result);
                    return builder.Build();
                }).ConfigureAwait(false);
            }
        }

        public async Task WriteExecutionError(string execution, string error)
        {
            await ExecutionsCache.OptimisticUpdateAsync(execution, IgniteBinary, value => { value.Finished = true; value.Error = error; }).ConfigureAwait(false);
        }

        public async Task<object?[]> ReadExecutionParameters(string execution, bool keepBinary = false)
        {
            if (!keepBinary)
            {
                return (await ExecutionsCache.GetAsync(execution).ConfigureAwait(false)).Parameters;
            }
            else
            {
                return (await ExecutionsCache.WithKeepBinary<string, IBinaryObject>().GetAsync(execution).ConfigureAwait(false))
                    .GetField<object[]>(nameof(ExecutionData.Parameters));
            }
        }

        public async Task<string?> ReadExecutionError(string execution, bool keepBinary = false)
        {
            if (!keepBinary)
            {
                return (await ExecutionsCache.GetAsync(execution).ConfigureAwait(false)).Error;
            }
            else
            {
                return (await ExecutionsCache.WithKeepBinary<string, IBinaryObject>().GetAsync(execution).ConfigureAwait(false))
                    .GetField<string>(nameof(ExecutionData.Error));
            }
        }

        public async Task<(string?, object?[]?)> ReadExecutionErrorAndResult(string execution, bool keepBinary = false)
        {
            if (!keepBinary)
            {
                var executionData = await ExecutionsCache.GetAsync(execution).ConfigureAwait(false);

                return (executionData.Error, executionData.Result);
            }
            else
            {
                var executionData = await ExecutionsCache.WithKeepBinary<string, IBinaryObject>().GetAsync(execution).ConfigureAwait(false);

                return (executionData.GetField<string>(nameof(ExecutionData.Error)), executionData.GetField<object[]>(nameof(ExecutionData.Result)));
            }
        }
    }
}