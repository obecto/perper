using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;

using Grpc.Core;

using Perper.Model;
using Perper.Protocol.Cache;
using Perper.Protocol.Protobuf;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperExecutions
    {
        (PerperExecution Execution, DelayedCreateFunc Start) IPerperExecutions.Create(PerperInstance instance, string @delegate, ParameterInfo[]? parameters)
        {
            var execution = new PerperExecution(GenerateName(@delegate));
            return (execution, async (arguments) =>
            {
                var packedArguments = FabricCaster.PackArguments(parameters, arguments);
                var executionData = new ExecutionData(instance.Agent, instance.Instance, @delegate, packedArguments);
                await ExecutionsCache.PutIfAbsentOrThrowAsync(execution.Execution, executionData).ConfigureAwait(false);
            }
            );
        }

        async Task IPerperExecutions.WriteResultAsync(PerperExecution execution) =>
            await ExecutionsCache.OptimisticUpdateAsync(execution.Execution, IgniteBinary, value =>
            {
                value.Finished = true;
            }).ConfigureAwait(false);

        async Task IPerperExecutions.WriteResultAsync<TResult>(PerperExecution execution, TResult result)
        {
            // NOTE: Using builder directly to allow for binary objects to be passed and directly unwrapped in the result; otherwise, Ignite will not unwrap them on the receiving side.
            var packedResult = FabricCaster.PackResult(result);
            await ExecutionsCache.WithKeepBinary<string, IBinaryObject>().OptimisticUpdateAsync(execution.Execution, binaryObject =>
            {
                var builder = binaryObject.ToBuilder();
                builder.SetField("Finished", true);
                builder.SetField("Result", packedResult);
                return builder.Build();
            }).ConfigureAwait(false);
        }

        async Task IPerperExecutions.WriteExceptionAsync(PerperExecution execution, Exception exception)
        {
            var packedException = FabricCaster.PackException(exception);
            await ExecutionsCache.OptimisticUpdateAsync(execution.Execution, IgniteBinary, value =>
            {
                value.Finished = true;
                value.Error = packedException;
            }).ConfigureAwait(false);
        }

        public async Task<object?[]> GetArgumentsAsync(PerperExecution execution, ParameterInfo[]? parameters)
        {
            var executionData = await ExecutionsCache.GetAsync(execution.Execution).ConfigureAwait(false);
            return FabricCaster.UnpackArguments(parameters, executionData.Parameters);
        }

        async Task IPerperExecutions.GetResultAsync(PerperExecution execution, CancellationToken cancellationToken)
        {
            await WaitExecutionFinished(execution, cancellationToken).ConfigureAwait(false);
            var executionData = await ExecutionsCache.GetAsync(execution.Execution).ConfigureAwait(false);
            if (executionData.Error != null)
            {
                throw FabricCaster.UnpackException(executionData.Error);
            }
        }

        async Task<TResult> IPerperExecutions.GetResultAsync<TResult>(PerperExecution execution, CancellationToken cancellationToken)
        {
            await WaitExecutionFinished(execution, cancellationToken).ConfigureAwait(false);
            var executionData = await ExecutionsCache.GetAsync(execution.Execution).ConfigureAwait(false);
            if (executionData.Error != null)
            {
                throw FabricCaster.UnpackException(executionData.Error);
            }
            return FabricCaster.UnpackResult<TResult>(executionData.Result);
        }

        private async Task WaitExecutionFinished(PerperExecution execution, CancellationToken cancellationToken = default) =>
            await FabricClient.ExecutionFinishedAsync(new ExecutionFinishedRequest
            {
                Execution = execution.Execution
            }, CallOptions.WithCancellationToken(cancellationToken));

        async Task IPerperExecutions.DestroyAsync(PerperExecution execution) => await ExecutionsCache.RemoveAsync(execution.Execution).ConfigureAwait(false);

        async IAsyncEnumerable<PerperExecutionData> IPerperExecutions.ListenAsync(PerperExecutionFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            async IAsyncEnumerable<ExecutionsResponse> Helper()
            {
                var executionsRequest = new ExecutionsRequest
                {
                    Agent = filter.Agent,
                    Instance = filter.Instance ?? "",
                    Delegate = filter.Delegate ?? ""
                };

                if (filter.Reserve)
                {
                    var batchSize = (ulong)1; // TODO: Make configurable (PerperExecutionFilter subclass?)

                    var stream = FabricClient.ReservedExecutions(CallOptions.WithCancellationToken(cancellationToken));

                    await stream.RequestStream.WriteAsync(new ReservedExecutionsRequest
                    {
                        ReserveNext = batchSize,
                        WorkGroup = Configuration.Workgroup,
                        Filter = executionsRequest
                    }).ConfigureAwait(false);

                    while (await stream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                    {
                        var executionProto = stream.ResponseStream.Current;
                        yield return executionProto;
                        if (!executionProto.Cancelled && !string.IsNullOrEmpty(executionProto.Execution))
                        {
                            await stream.RequestStream.WriteAsync(new ReservedExecutionsRequest
                            {
                                ReserveNext = 1, // TODO: With larger batch sizes, send only when the batch is about to run out.
                            }).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    var stream = FabricClient.Executions(executionsRequest, CallOptions.WithCancellationToken(cancellationToken));

                    while (await stream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                    {
                        var executionProto = stream.ResponseStream.Current;
                        yield return executionProto;
                    }
                }
            }

            var cancellationTokenSources = new Dictionary<string, CancellationTokenSource>();

            await foreach (var executionProto in Helper())
            {
                if (executionProto.Cancelled)
                {
                    if (cancellationTokenSources.TryGetValue(executionProto.Execution, out var cts))
                    {
                        cts.Cancel();
                    }
                }
                else if (!string.IsNullOrEmpty(executionProto.Execution))
                {
                    var cts = new CancellationTokenSource();
                    cancellationTokenSources[executionProto.Execution] = cts;
                    var execution = new PerperExecution(executionProto.Execution);
                    yield return new PerperExecutionData(
                        new PerperInstance(filter.Agent, executionProto.Instance),
                        executionProto.Delegate,
                        execution,
                        await GetArgumentsAsync(execution, filter.Parameters).ConfigureAwait(false),
                        cts.Token);
                }
            }
        }
    }
}