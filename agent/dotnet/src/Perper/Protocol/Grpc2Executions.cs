using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Options;

using Perper.Model;
using Perper.Protocol.Protobuf2;

using FabricExecutionsClient = Perper.Protocol.Protobuf2.FabricExecutions.FabricExecutionsClient;
using WellKnownTypes = Google.Protobuf.WellKnownTypes;

namespace Perper.Protocol
{
    public sealed class Grpc2Executions : IPerperExecutions
    {
        public Grpc2Executions(GrpcChannel grpcChannel, Grpc2TypeResolver grpc2TypeResolver, IGrpc2Caster grpc2Caster, IOptions<FabricConfiguration> configuration)
        {
            FabricExecutionsClient = new FabricExecutionsClient(grpcChannel);
            Grpc2TypeResolver = grpc2TypeResolver;
            Grpc2Caster = grpc2Caster;
            Configuration = configuration.Value;
        }

        private readonly FabricExecutionsClient FabricExecutionsClient;
        private readonly CallOptions CallOptions = new CallOptions().WithWaitForReady();
        private readonly Grpc2TypeResolver Grpc2TypeResolver;
        private readonly IGrpc2Caster Grpc2Caster;
        private readonly FabricConfiguration Configuration;

        private static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";
        (PerperExecution Execution, DelayedCreateFunc Start) IPerperExecutions.Create(PerperInstance instance, string @delegate, ParameterInfo[]? parameters)
        {
            var execution = new PerperExecution { Execution = GenerateName(@delegate) };
            return (execution, async (arguments) =>
                await FabricExecutionsClient.CreateAsync(new()
                {
                    Execution = execution,
                    Instance = instance,
                    Delegate = @delegate,
                    Arguments = { await Task.WhenAll(Grpc2Caster.PackArguments(parameters, arguments).Select(Grpc2TypeResolver.SerializeAny)).ConfigureAwait(false) }
                }, CallOptions));
        }

        async Task IPerperExecutions.WriteResultAsync(PerperExecution execution) =>
            await FabricExecutionsClient.CompleteAsync(new()
            {
                Execution = execution,
            }, CallOptions);

        async Task IPerperExecutions.WriteResultAsync<TResult>(PerperExecution execution, TResult result)
        {
            var packedResult = Grpc2Caster.PackResult(result);
            await FabricExecutionsClient.CompleteAsync(new()
            {
                Execution = execution,
                Results = { packedResult != null ? await Task.WhenAll(packedResult.Select(Grpc2TypeResolver.SerializeAny)).ConfigureAwait(false) : Enumerable.Empty<WellKnownTypes.Any>() }
            }, CallOptions);
        }

        async Task IPerperExecutions.WriteExceptionAsync(PerperExecution execution, Exception exception) =>
            await FabricExecutionsClient.CompleteAsync(new()
            {
                Execution = execution,
                Error = Grpc2Caster.SerializeException(exception)
            }, CallOptions);

        async Task IPerperExecutions.GetResultAsync(PerperExecution execution, CancellationToken cancellationToken)
        {
            var result = await FabricExecutionsClient.GetResultAsync(new()
            {
                Execution = execution
            }, CallOptions);

            if (result.Error != null)
            {
                throw Grpc2Caster.DeserializeException(result.Error);
            }
        }

        async Task<TResult> IPerperExecutions.GetResultAsync<TResult>(PerperExecution execution, CancellationToken cancellationToken)
        {
            var result = await FabricExecutionsClient.GetResultAsync(new()
            {
                Execution = execution
            }, CallOptions);

            if (result.Error != null)
            {
                throw Grpc2Caster.DeserializeException(result.Error);
            }
            return Grpc2Caster.UnpackResult<TResult>(await Task.WhenAll(result.Results.Select(x => Grpc2TypeResolver.DeserializeAny(x, typeof(object)))).ConfigureAwait(false));
        }

        async Task IPerperExecutions.DestroyAsync(PerperExecution execution) =>
            await FabricExecutionsClient.DeleteAsync(new()
            {
                Execution = execution
            }, CallOptions);

        async IAsyncEnumerable<PerperExecutionData> IPerperExecutions.ListenAsync(PerperExecutionFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            async IAsyncEnumerable<ExecutionsListenResponse> Helper()
            {
                var listenRequest = new ExecutionsListenRequest
                {
                    InstanceFilter = new PerperInstance(filter.Agent, filter.Instance ?? ""),
                    Delegate = filter.Delegate ?? ""
                };

                if (!filter.Reserve)
                {
                    var stream = FabricExecutionsClient.Listen(listenRequest, CallOptions.WithCancellationToken(cancellationToken));

                    while (await stream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                    {
                        yield return stream.ResponseStream.Current;
                    }
                }
                else
                {
                    var batchSize = (ulong)1; // TODO: Make configurable

                    var stream = FabricExecutionsClient.ListenAndReserve(CallOptions.WithCancellationToken(cancellationToken));

                    await stream.RequestStream.WriteAsync(new()
                    {
                        ReserveNext = batchSize,
                        Filter = listenRequest,
                        Workgroup = Configuration.Workgroup,
                    }).ConfigureAwait(false);

                    while (await stream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                    {
                        var executionProto = stream.ResponseStream.Current;
                        yield return executionProto;
                        if (!executionProto.Deleted && executionProto.Execution != null)
                        {
                            await stream.RequestStream.WriteAsync(new()
                            {
                                ReserveNext = 1, // TODO: With larger batch sizes, send only when the batch is about to run out.
                            }).ConfigureAwait(false);
                        }
                    }
                }
            }

            var cancellationTokenSources = new Dictionary<PerperExecution, CancellationTokenSource>();

            await foreach (var executionProto in Helper())
            {
                if (executionProto.Deleted)
                {
                    if (cancellationTokenSources.TryGetValue(executionProto.Execution!, out var cts))
                    {
                        cts.Cancel();
                    }
                }
                else if (executionProto.Execution != null)
                {
                    var cts = new CancellationTokenSource();
                    cancellationTokenSources[executionProto.Execution] = cts;
                    yield return new PerperExecutionData(
                        executionProto.Instance,
                        executionProto.Delegate,
                        executionProto.Execution,
                        Grpc2Caster.UnpackArguments(filter.Parameters, await Task.WhenAll(executionProto.Arguments.Select(x => Grpc2TypeResolver.DeserializeAny(x, typeof(object)))).ConfigureAwait(false)),
                        cts.Token);
                }
            }
        }
    }
}