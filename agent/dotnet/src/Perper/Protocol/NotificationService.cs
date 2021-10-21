using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Grpc.Core;
using Grpc.Net.Client;

using Perper.Protocol.Protobuf;

namespace Perper.Protocol
{
    public partial class NotificationService : IAsyncDisposable, IDisposable
    {
        public NotificationService(GrpcChannel grpcChannel) => FabricClient = new Fabric.FabricClient(grpcChannel);

        private readonly Fabric.FabricClient FabricClient;
        private readonly CallOptions CallOptions = new CallOptions().WithWaitForReady();
        private readonly CancellationTokenSource CancellationTokenSource = new();
        private readonly TaskCollection TaskCollection = new();
        private readonly ConcurrentDictionary<(string agent, string? instance), ExecutionsListener> ExecutionsListeners = new();

        public async ValueTask DisposeAsync()
        {
            CancellationTokenSource.Cancel();
            await TaskCollection.GetTask().ConfigureAwait(false);
        }

        public ChannelReader<ExecutionRecord> GetExecutionsReader(string agent, string? instance, string @delegate)
        {
            return ExecutionsListeners.GetOrAdd((agent, instance), _ => new(this, agent, instance)).GetReader(@delegate);
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

        public async IAsyncEnumerable<long> EnumerateStreamItemKeys(string stream, long startKey = -1, long stride = 0, bool localToData = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var streamItems = FabricClient.StreamItems(new StreamItemsRequest
            {
                Stream = stream,
                StartKey = startKey,
                Stride = stride,
                LocalToData = localToData
            }, CallOptions.WithCancellationToken(cancellationToken));

            while (await streamItems.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var item = streamItems.ResponseStream.Current;
                yield return item.Key;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancellationTokenSource.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~NotificationService()
        {
            Dispose(false);
        }

        private class ExecutionsListener
        {
            private readonly NotificationService NotificationService;
            private readonly string Agent;
            private readonly string? Instance;

            private readonly Dictionary<string, (ExecutionRecord execution, CancellationTokenSource cts)> Executions = new();
            private readonly ConcurrentDictionary<string, Channel<ExecutionRecord>> Channels = new();

            private int Running = 0;

            public ExecutionsListener(NotificationService notificationService, string agent, string? instance)
            {
                NotificationService = notificationService;
                Agent = agent;
                Instance = instance;
            }

            public void EnsureRunning()
            {
                if (Interlocked.CompareExchange(ref Running, 1, 0) == 0) {
                    NotificationService.TaskCollection.Add(RunAsync());
                }
            }

            public ChannelReader<ExecutionRecord> GetReader(string @delegate)
            {
                EnsureRunning();
                return Channels.GetOrAdd(@delegate, _ => Channel.CreateUnbounded<ExecutionRecord>()).Reader;
            }

            private async Task RunAsync()
            {
                var cancellationToken = NotificationService.CancellationTokenSource.Token;

                var stream = NotificationService.FabricClient.Executions(new ExecutionsRequest {
                    Agent = Agent,
                    Instance = Instance ?? ""
                }, NotificationService.CallOptions.WithCancellationToken(cancellationToken));

                while (await stream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    var executionProto = stream.ResponseStream.Current;
                    if (executionProto.Cancelled) {
                        if (Executions.TryGetValue(executionProto.Execution, out var execution)) {
                            execution.cts.Cancel();
                        }
                    } else {
                        var cts = new CancellationTokenSource();
                        var execution = new ExecutionRecord(Agent, executionProto.Instance, executionProto.Delegate, executionProto.Execution, cts.Token);
                        Executions[executionProto.Execution] = (execution, cts);
                        var channel = Channels.GetOrAdd(execution.Delegate, _ => Channel.CreateUnbounded<ExecutionRecord>());
                        await channel.Writer.WriteAsync(execution, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}