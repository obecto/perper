using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Grpc.Core;

using Perper.Model;
using Perper.Protocol.Cache;
using Perper.Protocol.Protobuf;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperExecutions
    {
        (PerperExecution Execution, DelayedCreateFunc Start) IPerperExecutions.Create(PerperAgent agent, string @delegate, ParameterInfo[]? parameters)
        {
            var execution = new PerperExecution(GenerateName(@delegate));
            return (execution, async (arguments) =>
            {
                var packedArguments = FabricCaster.PackArguments(parameters, arguments);
                var executionData = new ExecutionData(agent.Agent, agent.Instance, @delegate, packedArguments);
                await ExecutionsCache.PutIfAbsentOrThrowAsync(execution.Execution, executionData).ConfigureAwait(false);
            }
            );
        }

        async Task IPerperExecutions.WriteResultAsync(PerperExecution execution)
        {
            await ExecutionsCache.OptimisticUpdateAsync(execution.Execution, IgniteBinary, value =>
            {
                value.Finished = true;
            }).ConfigureAwait(false);
        }

        async Task IPerperExecutions.WriteResultAsync<TResult>(PerperExecution execution, TResult result)
        {
            var packedResult = FabricCaster.PackResult(result);
            await ExecutionsCache.OptimisticUpdateAsync(execution.Execution, IgniteBinary, value =>
            {
                value.Finished = true;
                value.Result = packedResult;
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

        async Task<object?[]> IPerperExecutions.GetArgumentsAsync(PerperExecution execution, ParameterInfo[]? parameters)
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

        private async Task WaitExecutionFinished(PerperExecution execution, CancellationToken cancellationToken = default)
        {
            await FabricClient.ExecutionFinishedAsync(new ExecutionFinishedRequest
            {
                Execution = execution.Execution
            }, CallOptions.WithCancellationToken(cancellationToken));
        }

        async Task IPerperExecutions.DestroyAsync(PerperExecution execution)
        {
            await ExecutionsCache.RemoveAsync(execution.Execution).ConfigureAwait(false);
        }

        private readonly ConcurrentDictionary<(string, string?), bool> RunningExecutionListeners = new();
        private readonly ConcurrentDictionary<PerperExecutionFilter, Channel<PerperExecutionData>> ExecutionChannels = new();

        IAsyncEnumerable<PerperExecutionData> IPerperExecutions.ListenAsync(PerperExecutionFilter filter, CancellationToken cancellationToken)
        {
            if (filter.Delegate != null)
            {
                if (RunningExecutionListeners.TryAdd((filter.Agent, filter.Instance), true))
                {
                    TaskCollection.Add(async () =>
                    {
                        await foreach (var data in ListenAsyncHelper(filter.Agent, filter.Instance, CancellationTokenSource.Token))
                        {
                            var channel = ExecutionChannels.GetOrAdd(filter with { Delegate = data.Delegate }, _ => Channel.CreateUnbounded<PerperExecutionData>());
                            await channel.Writer.WriteAsync(data).ConfigureAwait(false);
                        }
                    });
                }

                var resultChannel = ExecutionChannels.GetOrAdd(filter, _ => Channel.CreateUnbounded<PerperExecutionData>());
                return resultChannel.Reader.ReadAllAsync(cancellationToken);
            }
            else
            {
                return ListenAsyncHelper(filter.Agent, filter.Instance, cancellationToken);
            }
        }

        private async IAsyncEnumerable<PerperExecutionData> ListenAsyncHelper(string agent, string? instance, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var cancellationTokenSources = new Dictionary<string, CancellationTokenSource>();

            var stream = FabricClient.Executions(new ExecutionsRequest
            {
                Agent = agent,
                Instance = instance ?? "",
            }, CallOptions.WithCancellationToken(cancellationToken));

            while (await stream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var executionProto = stream.ResponseStream.Current;
                if (executionProto.Cancelled)
                {
                    if (cancellationTokenSources.TryGetValue(executionProto.Execution, out var cts))
                    {
                        cts.Cancel();
                    }
                }
                else
                {
                    var cts = new CancellationTokenSource();
                    cancellationTokenSources[executionProto.Execution] = cts;
                    yield return new PerperExecutionData(new PerperAgent(agent, executionProto.Instance), executionProto.Delegate, new PerperExecution(executionProto.Execution), cts.Token);
                }
            }
        }
    }
}