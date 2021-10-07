using System;
using System.Collections.Concurrent;
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

        private readonly ConcurrentDictionary<string, Channel<(NotificationKey, CallTriggerNotification)>> callTriggerChannels = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<(NotificationKey, CallResultNotification)>> callResultChannels = new();
        private readonly ConcurrentDictionary<string, Channel<(NotificationKey, StreamTriggerNotification)>> streamTriggerChannels = new();
        private readonly ConcurrentDictionary<(string, int?), Channel<(NotificationKey, StreamItemNotification)>> streamItemChannels = new();

        private Task? runningTask;
        private CancellationTokenSource? runningTaskCancellation;
        private AsyncServerStreamingCall<NotificationProto>? notificationsStream;

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

            notificationsStream = client.Notifications(new NotificationFilter { Agent = Agent, Instance = Instance ?? "" }, null, null, runningTaskCancellation.Token);
            await notificationsStream.ResponseHeadersAsync.ConfigureAwait(false);

            runningTask = RunAsync(runningTaskCancellation.Token);
        }

        private async Task RunAsync(CancellationToken cancellationToken = default)
        {
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
                    case StreamTriggerNotification st:
                        var channelSt = streamTriggerChannels.GetOrAdd(st.Delegate, _ => Channel.CreateUnbounded<(NotificationKey, StreamTriggerNotification)>());
                        await channelSt.Writer.WriteAsync((key, st), cancellationToken).ConfigureAwait(false);
                        break;
                    case CallTriggerNotification ct:
                        var channelCt = callTriggerChannels.GetOrAdd(ct.Delegate, _ => Channel.CreateUnbounded<(NotificationKey, CallTriggerNotification)>());
                        await channelCt.Writer.WriteAsync((key, ct), cancellationToken).ConfigureAwait(false);
                        break;
                    case CallResultNotification cr:
                        var taskCompletionSourceCr = callResultChannels.GetOrAdd(cr.Call, _ => new TaskCompletionSource<(NotificationKey, CallResultNotification)>());
                        if (!taskCompletionSourceCr.TrySetResult((key, cr)))
                        {
                            Console.WriteLine($"FabricService multiple completions: {cr.Call}");
                        }
                        break;
                }
            }
        }

        // TODO: Pass CancellationToken argument
        public async Task StopAsync()
        {
            runningTaskCancellation.Cancel();
            await runningTask.ConfigureAwait(false);
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
                runningTaskCancellation?.Dispose();
                runningTaskCancellation = null;
            }
        }

        public ChannelReader<(NotificationKey, CallTriggerNotification)> GetCallTriggerNotifications(string @delegate)
        {
            return callTriggerChannels.GetOrAdd(@delegate, _ => Channel.CreateUnbounded<(NotificationKey, CallTriggerNotification)>()).Reader;
        }

        public ChannelReader<(NotificationKey, StreamTriggerNotification)> GetStreamTriggerNotifications(string @delegate)
        {
            return streamTriggerChannels.GetOrAdd(@delegate, _ => Channel.CreateUnbounded<(NotificationKey, StreamTriggerNotification)>()).Reader;
        }

        public ChannelReader<(NotificationKey, StreamItemNotification)> GetStreamItemNotifications(string stream, int parameter)
        {
            return streamItemChannels.GetOrAdd((stream, parameter), _ => Channel.CreateUnbounded<(NotificationKey, StreamItemNotification)>()).Reader;
        }

        public async Task<(NotificationKey, CallResultNotification)> GetCallResultNotification(string call)
        {
            // HACK: Workaround race condition in CallResultNotification
            return await callResultChannels.GetOrAdd(call, _ => new TaskCompletionSource<(NotificationKey, CallResultNotification)>()).Task.ConfigureAwait(false);
            /*var notification = await client.CallResultNotificationAsync(new CallNotificationFilter
            {
                Agent = Agent,
                Call = call
            }, cancellationToken: cancellationToken);
            var key = GetNotificationKey(notification);

            var fullNotification = await notificationsCache.GetAsync(key).ConfigureAwait(false);
            return (key, (CallResultNotification)fullNotification);*/
        }

        public Task ConsumeNotification(NotificationKey key)
        {
            return notificationsCache.RemoveAsync(key);
        }
    }
}