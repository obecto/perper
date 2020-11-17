using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Perper.WebJobs.Extensions.Protobuf;
using Grpc.Core;
#if NETSTANDARD2_0
using System;
using Perper.WebJobs.Extensions.Model;
using GrpcChannel = Grpc.Core.Channel;
using Channel = System.Threading.Channels.Channel;
#else
using Grpc.Net.Client;
#endif
using Perper.WebJobs.Extensions.Cache.Notifications;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Core.Cache.Affinity;
using Notification = Perper.WebJobs.Extensions.Cache.Notifications.Notification;
using NotificationProto = Perper.WebJobs.Extensions.Protobuf.Notification;

namespace Perper.WebJobs.Extensions.Services
{
    public class FabricService : IHostedService
    {
        public string AgentDelegate { get; }

        private GrpcChannel _grpcChannel;
        private readonly Dictionary<(string, string?), Channel<(AffinityKey, Notification)>> _channels = new Dictionary<(string, string?), Channel<(AffinityKey, Notification)>>();
        private readonly IIgniteClient _ignite;
        private readonly ICacheClient<AffinityKey, Notification> _notificationsCache;
        private CancellationTokenSource _serviceCancellation = new CancellationTokenSource();
        private Task? _serviceTask;

        public FabricService(IIgniteClient ignite, string agentDelegate)
        {
            AgentDelegate = agentDelegate;
            _ignite = ignite;
            _notificationsCache = _ignite.GetCache<AffinityKey, Notification>($"$notifications-{AgentDelegate}");
            string address = $"http://{ignite.RemoteEndPoint}:40400";
#if NETSTANDARD2_0
            _grpcChannel = new GrpcChannel(address, ChannelCredentials.Insecure);
#else
            _grpcChannel = GrpcChannel.ForAddress(address);
#endif
        }

        public Task StartAsync(CancellationToken token)
        {
            _serviceCancellation = new CancellationTokenSource();
            _serviceTask = RunAsync(_serviceCancellation.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            _serviceCancellation.Cancel();
            return _serviceTask ?? Task.CompletedTask;
        }

        private AffinityKey GetAffinityKey(NotificationProto notification) {
            return new AffinityKey(
                notification.NotificationKey,
                notification.AffinityCase switch {
                    NotificationProto.AffinityOneofCase.StringAffinity => notification.StringAffinity,
                    NotificationProto.AffinityOneofCase.IntAffinity => notification.IntAffinity,
                    _ => null,
                });
        }

        private Channel<(AffinityKey, Notification)> GetChannel(string instance, string? parameter = null) {
            var key = (instance, parameter);
            if (_channels.TryGetValue(key, out var channel)) {
                return channel;
            }
            var newChannel = Channel.CreateUnbounded<(AffinityKey, Notification)>();
            _channels[key] = newChannel;
            return newChannel;
        }

        private async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var client = new Fabric.FabricClient(_grpcChannel);
            using var notifications = client.Notifications(new NotificationFilter {AgentDelegate = AgentDelegate}, null, null, cancellationToken);
            var stream = notifications.ResponseStream;
            while(await stream.MoveNext())
            {
                var key = GetAffinityKey(stream.Current);
                var notification = await _notificationsCache.GetAsync(key);
                switch (notification) {
                    case StreamItemNotification si:
                        await GetChannel(si.Stream, si.Parameter).Writer.WriteAsync((key, notification));
                        break;
                    case StreamTriggerNotification st:
                        await GetChannel(st.Stream).Writer.WriteAsync((key, notification));
                        break;
                    case CallTriggerNotification ct:
                        await GetChannel(ct.Call).Writer.WriteAsync((key, notification));
                        break;
                    case CallResultNotification cr:
                        // pass
                        break;
                }
            }
        }

        public Task ConsumeNotification(AffinityKey key, CancellationToken cancellationToken = default)
        {
            return _notificationsCache.RemoveAsync(key);
        }

#if NETSTANDARD2_0
        public class ChannelAsyncEnumerable<T> : IAsyncEnumerable<T>
        {
            ChannelReader<T> _reader;
            public ChannelAsyncEnumerable(ChannelReader<T> reader)
            {
                _reader = reader;
            }

            public async Task ForEachAwaitAsync(Func<T, Task> action, CancellationToken cancellationToken)
            {
                while (true)
                {
                    var value = await _reader.ReadAsync(cancellationToken);
                    await action(value);
                }
            }
        }

        public IAsyncEnumerable<(AffinityKey, Notification)> GetNotifications(string instance, string? parameter = null, CancellationToken cancellationToken = default)
        {
            return new ChannelAsyncEnumerable<(AffinityKey, Notification)>(GetChannel(instance, parameter).Reader);
        }
#else
        public IAsyncEnumerable<(AffinityKey, Notification)> GetNotifications(string instance, string? parameter = null, CancellationToken cancellationToken = default)
        {
            return GetChannel(instance, parameter).Reader.ReadAllAsync(cancellationToken);
        }
#endif
        public async Task<(AffinityKey, Notification)> GetCallNotification(string call, CancellationToken cancellationToken = default)
        {
            var client = new Fabric.FabricClient(_grpcChannel);
            var notification = await client.CallResultNotificationAsync(new CallNotificationFilter {
                AgentDelegate = AgentDelegate,
                CallName = call
            });
            var key = GetAffinityKey(notification);
            var fullNotification = await _notificationsCache.GetAsync(key);
            return (key, fullNotification);
        }
    }
}