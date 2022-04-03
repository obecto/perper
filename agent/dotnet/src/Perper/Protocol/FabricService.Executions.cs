using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

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
                await _executionsCache.PutIfAbsentOrThrowAsync(execution.Execution, executionData).ConfigureAwait(false);
            }
            );
        }

        async Task IPerperExecutions.WriteResultAsync(PerperExecution execution) =>
            await _executionsCache.OptimisticUpdateAsync(execution.Execution, _igniteBinary, value =>
            {
                value.Finished = true;
            }).ConfigureAwait(false);

        async Task IPerperExecutions.WriteResultAsync<TResult>(PerperExecution execution, TResult result)
        {
            var packedResult = FabricCaster.PackResult(result);
            await _executionsCache.OptimisticUpdateAsync(execution.Execution, _igniteBinary, value =>
            {
                value.Finished = true;
                value.Result = packedResult;
            }).ConfigureAwait(false);
        }

        async Task IPerperExecutions.WriteExceptionAsync(PerperExecution execution, Exception exception)
        {
            var packedException = FabricCaster.PackException(exception);
            await _executionsCache.OptimisticUpdateAsync(execution.Execution, _igniteBinary, value =>
            {
                value.Finished = true;
                value.Error = packedException;
            }).ConfigureAwait(false);
        }

        async Task<object?[]> IPerperExecutions.GetArgumentsAsync(PerperExecution execution, ParameterInfo[]? parameters)
        {
            var executionData = await _executionsCache.GetAsync(execution.Execution).ConfigureAwait(false);
            return FabricCaster.UnpackArguments(parameters, executionData.Parameters);
        }

        async Task IPerperExecutions.GetResultAsync(PerperExecution execution, CancellationToken cancellationToken)
        {
            await WaitExecutionFinished(execution, cancellationToken).ConfigureAwait(false);
            var executionData = await _executionsCache.GetAsync(execution.Execution).ConfigureAwait(false);
            if (executionData.Error != null)
            {
                throw FabricCaster.UnpackException(executionData.Error);
            }
        }

        async Task<TResult> IPerperExecutions.GetResultAsync<TResult>(PerperExecution execution, CancellationToken cancellationToken)
        {
            await WaitExecutionFinished(execution, cancellationToken).ConfigureAwait(false);
            var executionData = await _executionsCache.GetAsync(execution.Execution).ConfigureAwait(false);
            if (executionData.Error != null)
            {
                throw FabricCaster.UnpackException(executionData.Error);
            }
            return FabricCaster.UnpackResult<TResult>(executionData.Result);
        }

        private async Task WaitExecutionFinished(PerperExecution execution, CancellationToken cancellationToken = default) =>
            await _fabricClient.ExecutionFinishedAsync(new ExecutionFinishedRequest
            {
                Execution = execution.Execution
            }, _callOptions.WithCancellationToken(cancellationToken));

        async Task IPerperExecutions.DestroyAsync(PerperExecution execution) => await _executionsCache.RemoveAsync(execution.Execution).ConfigureAwait(false);

        private readonly ConcurrentDictionary<(string, string?), bool> _runningExecutionListeners = new();
        private readonly ConcurrentDictionary<PerperExecutionFilter, Channel<PerperExecutionData>> _executionChannels = new();

        IAsyncEnumerable<PerperExecutionData> IPerperExecutions.ListenAsync(PerperExecutionFilter filter, CancellationToken cancellationToken)
        {
            if (filter.Delegate != null)
            {
                if (_runningExecutionListeners.TryAdd((filter.Agent, filter.Instance), true))
                {
                    _taskCollection.Add(async () =>
                    {
                        await foreach (var data in ListenAsyncHelper(filter.Agent, filter.Instance, _cancellationTokenSource.Token))
                        {
                            var channel = _executionChannels.GetOrAdd(filter with { Delegate = data.Delegate }, _ => Channel.CreateUnbounded<PerperExecutionData>());
                            await channel.Writer.WriteAsync(data, cancellationToken).ConfigureAwait(false);
                        }
                    });
                }

                var resultChannel = _executionChannels.GetOrAdd(filter, _ => Channel.CreateUnbounded<PerperExecutionData>());
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

            var stream = _fabricClient.Executions(new ExecutionsRequest
            {
                Agent = agent,
                Instance = instance ?? "",
            }, _callOptions.WithCancellationToken(cancellationToken));

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