using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Grpc.Net.Client;
using Grpc.Core;

using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Protobuf;

using Notification = Perper.Protocol.Cache.Notifications.Notification;
using NotificationProto = Perper.Protocol.Protobuf.Notification;

namespace Perper.Protocol.Service
{
    public partial class NotificationService : IDisposable
    {
        public NotificationService(
            IIgniteClient ignite,
            GrpcChannel grpcChannel,
            string agent)
        {
            Agent = agent;
            notificationsCache = ignite.GetCache<NotificationKey, Notification>($"{Agent}-$notifications");
            client = new Fabric.FabricClient(grpcChannel);
        }

        public string Agent { get; }
        private readonly ICacheClient<NotificationKey, Notification> notificationsCache;
        private readonly Fabric.FabricClient client;

        private readonly ConcurrentDictionary<(string, int?), Channel<(NotificationKey, Notification)>> channels =
            new ConcurrentDictionary<(string, int?), Channel<(NotificationKey, Notification)>>();

        private Task? runningTask;
        private CancellationTokenSource? runningTaskCancellation;
        private AsyncServerStreamingCall<NotificationProto>? notificationsStream;

        private Channel<(NotificationKey, Notification)> GetChannel(string instance, int? parameter = null)
        {
            return channels.GetOrAdd((instance, parameter), _ =>
                Channel.CreateUnbounded<(NotificationKey, Notification)>());
        }

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

            notificationsStream = client.Notifications(new NotificationFilter { Agent = Agent }, null, null, runningTaskCancellation.Token);
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
                        await GetChannel(si.Stream, si.Parameter).Writer.WriteAsync((key, notification), cancellationToken).ConfigureAwait(false);
                        break;
                    case StreamTriggerNotification st:
                        await GetChannel(st.Delegate).Writer.WriteAsync((key, notification), cancellationToken).ConfigureAwait(false);
                        break;
                    case CallTriggerNotification ct:
                        await GetChannel(ct.Delegate).Writer.WriteAsync((key, notification), cancellationToken).ConfigureAwait(false);
                        break;
                    case CallResultNotification _:
                        // pass
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

        public IAsyncEnumerable<(NotificationKey, Notification)> GetNotifications(string instance, int? parameter = null, CancellationToken cancellationToken = default)
        {
            return GetChannel(instance, parameter).Reader.ReadAllAsync(cancellationToken);
        }

        public async Task<(NotificationKey, CallResultNotification)> GetCallResultNotification(string call, CancellationToken cancellationToken = default)
        {
            var notification = await client.CallResultNotificationAsync(new CallNotificationFilter
            {
                Agent = Agent,
                Call = call
            }, cancellationToken: cancellationToken);
            var key = GetNotificationKey(notification);

            var fullNotification = await notificationsCache.GetAsync(key).ConfigureAwait(false);
            return (key, (CallResultNotification)fullNotification);
        }

        public Task ConsumeNotification(NotificationKey key)
        {
            return notificationsCache.RemoveAsync(key);
        }
    }
}