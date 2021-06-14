using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Perper.Protocol.Cache;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Protobuf;
using Grpc.Net.Client;
using Notification = Perper.Protocol.Cache.Notifications.Notification;
using NotificationProto = Perper.Protocol.Protobuf.Notification;

namespace Perper.Protocol
{
    public class FabricService
    {
        public static IEnumerable<BinaryTypeConfiguration> BinaryTypeConfigurations = new BinaryTypeConfiguration[]
        {
            new BinaryTypeConfiguration(typeof(CallTriggerNotification)),
            new BinaryTypeConfiguration(typeof(CallResultNotification)),
            new BinaryTypeConfiguration(typeof(StreamTriggerNotification)),
            new BinaryTypeConfiguration(typeof(StreamItemNotification)),
        };

        public FabricService(
            IIgniteClient ignite,
            GrpcChannel grpcChannel,
            string agent)
        {
            this.agent = agent;
            notificationsCache = ignite.GetCache<NotificationKey, Notification>($"{agent}-$notifications");
            client = new Fabric.FabricClient(grpcChannel);
        }

        private string agent;
        private ICacheClient<NotificationKey, Notification> notificationsCache;
        private Fabric.FabricClient client;

        public static NotificationKey GetNotificationKey(NotificationProto notification)
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

        public async IAsyncEnumerable<(NotificationKey, Notification)> GetNotifications(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var notifications = client.Notifications(new NotificationFilter { Agent = agent }, null, null, cancellationToken);

            while (await notifications.ResponseStream.MoveNext(cancellationToken))
            {
                var key = GetNotificationKey(notifications.ResponseStream.Current);
                var notificationResult = await notificationsCache.TryGetAsync(key);

                if (!notificationResult.Success)
                {
                    Console.WriteLine($"FabricService failed to read notification: {key}");
                    continue;
                }

                yield return (key, notificationResult.Value);
            }
        }

        public async Task<(NotificationKey, CallResultNotification)> GetCallResultNotification(string call, CancellationToken cancellationToken = default)
        {
            var notification = await client.CallResultNotificationAsync(new CallNotificationFilter
            {
                Agent = agent,
                Call = call
            });
            var key = GetNotificationKey(notification);

            var fullNotification = await notificationsCache.GetAsync(key);
            return (key, (CallResultNotification)fullNotification);
        }

        public Task ConsumeNotification(NotificationKey key)
        {
            return notificationsCache.RemoveAsync(key);
        }
    }
}
