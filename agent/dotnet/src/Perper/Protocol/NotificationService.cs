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
    public partial class NotificationService : IDisposable
    {
        public NotificationService(
            GrpcChannel grpcChannel,
            string agent,
            string? instance)
        {
            Agent = agent;
            Instance = instance;
            client = new Fabric.FabricClient(grpcChannel);
        }

        public string Agent { get; }
        public string? Instance { get; }
        private readonly Fabric.FabricClient client;

        private readonly ConcurrentDictionary<string, Channel<(string instance, string call)>> callTriggerChannels = new();
        private readonly ConcurrentDictionary<string, Channel<(string instance, string stream)>> streamTriggerChannels = new();

        private Task? runningTask;
        private CancellationTokenSource? runningTaskCancellation;
        private AsyncServerStreamingCall<CallTrigger>? callTriggersStream;
        private AsyncServerStreamingCall<StreamTrigger>? streamTriggersStream;

        // TODO: Pass CancellationToken argument
        public async Task StartAsync()
        {
            runningTaskCancellation = new CancellationTokenSource();

            var cancellationToken = runningTaskCancellation.Token;

            // TODO: Refactor
            callTriggersStream = client.CallTriggers(new NotificationFilter { Agent = Agent, Instance = Instance ?? "" }, null, null, cancellationToken);
            streamTriggersStream = client.StreamTriggers(new NotificationFilter { Agent = Agent, Instance = Instance ?? "" }, null, null, cancellationToken);
            await callTriggersStream.ResponseHeadersAsync.ConfigureAwait(false);
            await streamTriggersStream.ResponseHeadersAsync.ConfigureAwait(false);

            runningTask = Task.WhenAll(ProcessCallTriggersAsync(cancellationToken), ProcessStreamTriggersAsync(cancellationToken));
        }

        private async Task ProcessCallTriggersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.Assert(callTriggersStream != null);

                while (await callTriggersStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    var trigger = callTriggersStream.ResponseStream.Current;
                    var channel = callTriggerChannels.GetOrAdd(trigger.Delegate, _ => Channel.CreateUnbounded<(string, string)>());
                    await channel.Writer.WriteAsync((trigger.Instance, trigger.Call), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task ProcessStreamTriggersAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.Assert(streamTriggersStream != null);

                while (await streamTriggersStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    var trigger = streamTriggersStream.ResponseStream.Current;
                    var channel = streamTriggerChannels.GetOrAdd(trigger.Delegate, _ => Channel.CreateUnbounded<(string, string)>());
                    await channel.Writer.WriteAsync((trigger.Instance, trigger.Stream), cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        // TODO: Pass CancellationToken argument
        public async Task StopAsync()
        {
            runningTaskCancellation?.Cancel();
            if (runningTask != null)
            {
                await runningTask.ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                callTriggersStream?.Dispose();
                callTriggersStream = null;
                streamTriggersStream?.Dispose();
                streamTriggersStream = null;
                runningTaskCancellation?.Dispose();
                runningTaskCancellation = null;
            }
        }

        public ChannelReader<(string instance, string call)> GetCallTriggers(string @delegate)
        {
            return callTriggerChannels.GetOrAdd(@delegate, _ => Channel.CreateUnbounded<(string, string)>()).Reader;
        }

        public ChannelReader<(string instance, string stream)> GetStreamTriggers(string @delegate)
        {
            return streamTriggerChannels.GetOrAdd(@delegate, _ => Channel.CreateUnbounded<(string, string)>()).Reader;
        }

        public async Task WaitCallFinished(string call, CancellationToken cancellationToken = default)
        {
            await client.CallFinishedAsync(new CallFilter
            {
                Call = call
            }, cancellationToken: cancellationToken);
        }

        public async IAsyncEnumerable<long> StreamItems(string stream, long startKey = -1, long stride = 0, bool localToData = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var streamItems = client.StreamItems(new StreamItemsRequest
            {
                Stream = stream,
                StartKey = startKey,
                Stride = stride,
                LocalToData = localToData
            }, cancellationToken: cancellationToken);

            while (await streamItems.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var item = streamItems.ResponseStream.Current;
                yield return item.Key;
            }
        }
    }
}