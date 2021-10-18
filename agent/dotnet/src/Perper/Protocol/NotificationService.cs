using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Grpc.Core;
using Grpc.Net.Client;

using Perper.Protocol.Notifications;
using Perper.Protocol.Protobuf;

using Notification = Perper.Protocol.Notifications.Notification;
using NotificationProto = Perper.Protocol.Protobuf.Notification;

namespace Perper.Protocol
{
    public partial class NotificationService : IDisposable
    {
        public NotificationService(
            IIgniteClient ignite,
            GrpcChannel grpcChannel,
            string agent,
            string? instance)
        {
            Agent = agent;
            Instance = instance;
            notificationsCache = ignite.GetCache<NotificationKey, Notification>($"{Agent}-$notifications");
            client = new Fabric.FabricClient(grpcChannel);
        }

        public string Agent { get; }
        public string? Instance { get; }
        private readonly ICacheClient<NotificationKey, Notification> notificationsCache;
        private readonly Fabric.FabricClient client;

        private readonly ConcurrentDictionary<string, Channel<(string instance, string call)>> callTriggerChannels = new();
        private readonly ConcurrentDictionary<string, Channel<(string instance, string stream)>> streamTriggerChannels = new();
        private readonly ConcurrentDictionary<(string, int?), Channel<(NotificationKey, StreamItemNotification)>> streamItemChannels = new();

        private Task? runningTask;
        private CancellationTokenSource? runningTaskCancellation;
        private AsyncServerStreamingCall<NotificationProto>? notificationsStream;
        private AsyncServerStreamingCall<CallTrigger>? callTriggersStream;
        private AsyncServerStreamingCall<StreamTrigger>? streamTriggersStream;

        private static NotificationKey GetNotificationKey(NotificationProto notification)
        {
            return (notification.AffinityCase switch
            {
                NotificationProto.AffinityOneofCase.StringAffinity => new NotificationKeyString(
                    notification.StringAffinity,
                    notification.NotificationKey),
                NotificationProto.AffinityOneofCase.IntAffinity => new NotificationKeyLong(
                    notification.IntAffinity,
                    notification.NotificationKey),
                _ => default!
            })!;
        }

        // TODO: Pass CancellationToken argument
        public async Task StartAsync()
        {
            runningTaskCancellation = new CancellationTokenSource();

            var cancellationToken = runningTaskCancellation.Token;

            // TODO: Refactor
            notificationsStream = client.Notifications(new NotificationFilter { Agent = Agent, Instance = Instance ?? "" }, null, null, cancellationToken);
            callTriggersStream = client.CallTriggers(new NotificationFilter { Agent = Agent, Instance = Instance ?? "" }, null, null, cancellationToken);
            streamTriggersStream = client.StreamTriggers(new NotificationFilter { Agent = Agent, Instance = Instance ?? "" }, null, null, cancellationToken);
            await notificationsStream.ResponseHeadersAsync.ConfigureAwait(false);
            await callTriggersStream.ResponseHeadersAsync.ConfigureAwait(false);
            await streamTriggersStream.ResponseHeadersAsync.ConfigureAwait(false);

            runningTask = Task.WhenAll(RunAsync(cancellationToken), ProcessCallTriggersAsync(cancellationToken), ProcessStreamTriggersAsync(cancellationToken));
        }

        private async Task RunAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Debug.Assert(notificationsStream != null);

                while (await notificationsStream.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
                {
                    var key = GetNotificationKey(notificationsStream.ResponseStream.Current);
                    var notificationResult = await notificationsCache.TryGetAsync(key).ConfigureAwait(false);

                    if (!notificationResult.Success)
                    {
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                        notificationResult = await notificationsCache.TryGetAsync(key).ConfigureAwait(false);
                        if (!notificationResult.Success)
                        {
                            Console.WriteLine($"FabricService failed to read notification: {key}");
                            continue;
                        }
                    }

                    var notification = notificationResult.Value;

                    switch (notification)
                    {
                        case StreamItemNotification si:
                            var channelSi = streamItemChannels.GetOrAdd((si.Stream, si.Parameter), _ => Channel.CreateUnbounded<(NotificationKey, StreamItemNotification)>());
                            await channelSi.Writer.WriteAsync((key, si), cancellationToken).ConfigureAwait(false);
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
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
                notificationsStream?.Dispose();
                notificationsStream = null;
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

        public ChannelReader<(NotificationKey, StreamItemNotification)> GetStreamItemNotifications(string stream, int parameter)
        {
            return streamItemChannels.GetOrAdd((stream, parameter), _ => Channel.CreateUnbounded<(NotificationKey, StreamItemNotification)>()).Reader;
        }

        public async Task WaitCallFinished(string call, CancellationToken cancellationToken = default)
        {
            await client.CallFinishedAsync(new CallFilter
            {
                Call = call
            }, cancellationToken: cancellationToken);
        }

        public Task ConsumeNotification(NotificationKey key)
        {
            if (key == null)
            {
                return Task.CompletedTask;
            }
            return notificationsCache.RemoveAsync(key);
        }
    }
}