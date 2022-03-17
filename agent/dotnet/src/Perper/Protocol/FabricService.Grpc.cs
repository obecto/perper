using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Grpc.Core;

using Perper.Protocol.Protobuf;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public async IAsyncEnumerable<FabricExecution> EnumerateExecutions(string agent, string? instance, CancellationToken cancellationToken = default)
        {
            var cancellationTokenSources = new Dictionary<string, CancellationTokenSource>();

            var stream = FabricClient.Executions(new ExecutionsRequest
            {
                Agent = agent,
                Instance = instance ?? ""
            }, CallOptions.WithCancellationToken(cancellationToken));

            while (await stream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var executionProto = stream.ResponseStream.Current;
                if (!executionProto.Cancelled)
                {
                    var cts = new CancellationTokenSource();
                    cancellationTokenSources[executionProto.Execution] = cts;
                    yield return new FabricExecution(agent, executionProto.Instance, executionProto.Delegate, executionProto.Execution, cts.Token);
                }
                else
                {
                    if (cancellationTokenSources.TryGetValue(executionProto.Execution, out var cts))
                    {
                        cts.Cancel();
                    }
                }
            }
        }

        public async Task WaitExecutionFinished(string execution, CancellationToken cancellationToken = default)
        {
            await FabricClient.ExecutionFinishedAsync(new ExecutionFinishedRequest
            {
                Execution = execution
            }, CallOptions.WithCancellationToken(cancellationToken));
        }

        public async Task WaitListenerAttached(string stream, CancellationToken cancellationToken = default)
        {
            await FabricClient.ListenerAttachedAsync(new ListenerAttachedRequest
            {
                Stream = stream
            }, CallOptions.WithCancellationToken(cancellationToken));
        }

        public async Task<IAsyncEnumerable<long>> EnumerateStreamItemKeys(string stream, long startKey = -1, long stride = 0, bool localToData = false, CancellationToken cancellationToken = default)
        {
            var streamItems = FabricClient.StreamItems(new StreamItemsRequest
            {
                Stream = stream,
                StartKey = startKey,
                Stride = stride,
                LocalToData = localToData
            }, CallOptions.WithCancellationToken(cancellationToken));

            await streamItems.ResponseHeadersAsync.ConfigureAwait(false);

            async IAsyncEnumerable<long> helper([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                while (await streamItems.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    var item = streamItems.ResponseStream.Current;
                    yield return item.Key;
                }
            }

            return helper(cancellationToken);
        }

        private readonly ConcurrentDictionary<(string agent, string? instance), ExecutionsListener> ExecutionsListeners = new();

        public ChannelReader<FabricExecution> GetExecutionsReader(string agent, string? instance, string @delegate)
        {
            return ExecutionsListeners.GetOrAdd((agent, instance), _ => new(this, agent, instance)).GetReader(@delegate);
        }

        private class ExecutionsListener
        {
            private readonly FabricService FabricService;
            private readonly string Agent;
            private readonly string? Instance;

            private readonly ConcurrentDictionary<string, Channel<FabricExecution>> Channels = new();

            private int Running = 0;

            public ExecutionsListener(FabricService fabricService, string agent, string? instance)
            {
                FabricService = fabricService;
                Agent = agent;
                Instance = instance;
            }

            public void EnsureRunning()
            {
                if (Interlocked.CompareExchange(ref Running, 1, 0) == 0)
                {
                    FabricService.TaskCollection.Add(RunAsync());
                }
            }

            public ChannelReader<FabricExecution> GetReader(string @delegate)
            {
                EnsureRunning();
                return Channels.GetOrAdd(@delegate, _ => Channel.CreateUnbounded<FabricExecution>()).Reader;
            }

            private async Task RunAsync()
            {
                var cancellationToken = FabricService.CancellationTokenSource.Token;
                await foreach (var execution in FabricService.EnumerateExecutions(Agent, Instance))
                {
                    var channel = Channels.GetOrAdd(execution.Delegate, _ => Channel.CreateUnbounded<FabricExecution>());
                    await channel.Writer.WriteAsync(execution, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}