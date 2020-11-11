using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Perper.WebJobs.Extensions.Protobuf;
using Grpc.Core;
#if NETSTANDARD2_0
using GrpcChannel = Grpc.Core.Channel;
#else
using System.Linq;
using Grpc.Net.Client;
#endif
using Perper.WebJobs.Extensions.Cache.Notifications;
using Notification = Perper.WebJobs.Extensions.Cache.Notifications.Notification;
using NotificationProto = Perper.WebJobs.Extensions.Protobuf.Notification;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Cache.Affinity;

namespace Perper.WebJobs.Extensions.Services
{
    public class FabricService
    {
        private GrpcChannel _grpcChannel;
        private Dictionary<(string, string?, string?), Channel<(AffinityKey, Notification)>> _channels = new Dictionary<(string, string?, string?), Channel<(AffinityKey, Notification)>>();
        private readonly IIgniteClient _ignite;

        public FabricService(IIgniteClient ignite)
        {
            _ignite = ignite;
            string address = $"http://{ignite.RemoteEndPoint}:40400";
#if NETSTANDARD2_0
            _grpcChannel = new GrpcChannel(address, ChannelCredentials.Insecure);
#else
            _grpcChannel = GrpcChannel.ForAddress(address);
#endif
        }

#if !NETSTANDARD2_0
        private AffinityKey GetAffinityKey(NotificationProto notification) {
            return new AffinityKey(
                notification.NotificationKey,
                notification.AffinityCase switch {
                    NotificationProto.AffinityOneofCase.StringAffinity => notification.StringAffinity,
                    NotificationProto.AffinityOneofCase.IntAffinity => notification.IntAffinity,
                    _ => null,
                });
        }

        private Channel<(AffinityKey, Notification)> GetChannel(string agentDelegate, string? stream = null, string? parameter = null) {
            var key = (agentDelegate, stream, parameter);
            if (_channels.TryGetValue(key, out var channel)) {
                return channel;
            }
            var newChannel = Channel.CreateUnbounded<(AffinityKey, Notification)>();
            _channels[key] = newChannel;
            return newChannel;
        }

        private async Task RunAsync(string delegateName, CancellationToken cancellationToken = default)
        {
            var client = new Fabric.FabricClient(_grpcChannel);
            using var notifications = client.Notifications(new NotificationFilter {AgentDelegate = delegateName}, null, null, cancellationToken);
            var notificationsCache = _ignite.GetCache<AffinityKey, Notification>($"$notifications-{delegateName}");
            await foreach (var notification in notifications.ResponseStream.ReadAllAsync(cancellationToken))
            {
                var key = GetAffinityKey(notification);
                var fullNotification = await notificationsCache.GetAsync(key);
                var channel = fullNotification switch {
                    StreamItemNotification si => GetChannel(delegateName, si.Stream, si.Parameter),
                    CallResultNotification cr => GetChannel(delegateName, null, cr.Call),
                    _ => GetChannel(delegateName)
                };
                await channel.Writer.WriteAsync((key, fullNotification));
            }
            // Hosted: use two separate streams for every delegate - one for runtime and one for model
            // Workers: separate GRPC connections
        }

        public Task ConsumeNotification(string delegateName, AffinityKey key, CancellationToken cancellationToken = default)
        {
            var notificationsCache = _ignite.GetCache<AffinityKey, Notification>($"$notifications-{delegateName}");
            return notificationsCache.RemoveAsync(key);
        }

        public IAsyncEnumerable<(AffinityKey, Notification)> GetNotifications(string delegateName, CancellationToken cancellationToken = default)
        {
            return GetChannel(delegateName).Reader.ReadAllAsync(cancellationToken);
        }

        public IAsyncEnumerable<(AffinityKey, Notification)> GetStreamItemNotifications(string delegateName, string stream, string parameter,
            CancellationToken cancellationToken = default)
        {
            return GetChannel(delegateName, stream, parameter).Reader.ReadAllAsync(cancellationToken);
        }

        public ValueTask<(AffinityKey, Notification)> GetCallNotification(string delegateName, string call, CancellationToken cancellationToken = default)
        {
            return GetChannel(delegateName, null, call).Reader.ReadAsync(cancellationToken);
        }
#endif
    }
}