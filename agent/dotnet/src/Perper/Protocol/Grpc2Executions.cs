
using Grpc.Net.Client;

using Microsoft.Extensions.Options;

using Perper.Model;
using Perper.Protocol.Protobuf2;

using FabricExecutionsClient = Perper.Protocol.Protobuf2.FabricExecutions.FabricExecutionsClient;

namespace Perper.Protocol
{
    public class Grpc2Executions : IPerperExecutions
    {
        public Grpc2Executions(GrpcChannel grpcChannel, Grpc2TypeResolver grpc2TypeResolver, IGrpc2Caster grpc2Caster)
        {
            FabricExecutionsClient = new FabricExecutionsClient(grpcChannel);
            Grpc2TypeResolver = grpc2TypeResolver;
            Grpc2Caster = grpc2Caster;
        }

        private readonly FabricExecutionsClient FabricExecutionsClient;
        private readonly CallOptions CallOptions = new CallOptions().WithWaitForReady();
        private readonly Grpc2TypeResolver Grpc2TypeResolver;
        private readonly IGrpc2Caster Grpc2Caster;
        private readonly Concurrent Grpc2Caster;

        //private static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";
        (PerperExecution Execution, DelayedCreateFunc Start) IPerperExecutions.Create(PerperInstance instance, string @delegate, ParameterInfo[]? parameters)
        {
            var execution = new PerperExecution { Execution = GenerateName(@delegate) };
            return (execution, async (arguments) =>
                await FabricExecutionsClient.CreateAsync(new ExecutionsCreateRequest()
                {
                    Execution = execution,
                    Instance = instance,
                    Delegate = @delegate,
                    Parameters = { await Task.WhenAll(Grpc2Caster.PackArguments(parameters, arguments).Select(Grpc2TypeResolver.SerializeAny))) }
                }, CallOptions).ConfigureAwait(false));
        }

        async Task IPerperExecutions.WriteResultAsync(PerperExecution execution) =>
            await FabricExecutionsClient.Complete(new ExecutionCompleteRequest() {
                Execution = execution,
            }, CallOptions).ConfigureAwait(false);

        async Task IPerperExecutions.WriteResultAsync<TResult>(PerperExecution execution, TResult result) =>
            await FabricExecutionsClient.Complete(new ExecutionCompleteRequest() {
                Execution = execution,
                Result = { await Task.WhenAll(FabricCaster.PackResult(result).Select(Grpc2TypeResolver.SerializeAny))) }
            }, CallOptions).ConfigureAwait(false);

        async Task IPerperExecutions.WriteExceptionAsync(PerperExecution execution, Exception exception) =>
            await FabricExecutionsClient.Complete(new ExecutionCompleteRequest() {
                Execution = execution,
                Error = Grpc2TypeResolver.SerializeException(exception)
            }, CallOptions).ConfigureAwait(false);

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
                    yield return new PerperExecutionData(new PerperAgent(filter.Agent, executionProto.Instance), executionProto.Delegate, new PerperExecution(executionProto.Execution), cts.Token);
                }
            }
        }
    }
    }
}