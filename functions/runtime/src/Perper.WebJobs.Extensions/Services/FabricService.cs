using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
#if NETSTANDARD2_0
using Grpc.Core;
using GrpcChannel = Grpc.Core.Channel;
using Channel = System.Threading.Channels.Channel;
#else
using Grpc.Net.Client;
#endif
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Perper.WebJobs.Extensions.Cache.Notifications;
using Perper.WebJobs.Extensions.Protobuf;
using Perper.WebJobs.Extensions.Config;
using Notification = Perper.WebJobs.Extensions.Cache.Notifications.Notification;
using NotificationProto = Perper.WebJobs.Extensions.Protobuf.Notification;

namespace Perper.WebJobs.Extensions.Services
{
    public class FabricService : IHostedService
    {
        public string AgentDelegate { get; }

        private readonly IIgniteClient _ignite;
        private ILogger _logger;

        private GrpcChannel _grpcChannel;
        private readonly ICacheClient<AffinityKey, Notification> _notificationsCache;
        private readonly Dictionary<(string, int?), Channel<(AffinityKey, Notification)>> _channels = new Dictionary<(string, int?), Channel<(AffinityKey, Notification)>>();

        private CancellationTokenSource _serviceCancellation = new CancellationTokenSource();
        private Task? _serviceTask;

        public FabricService(IIgniteClient ignite, IOptions<PerperConfig> config, ILogger<FabricService> logger)
        {
            var agentDelegate = Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())))!;
            var suffix = ".FunctionApp";
            if (agentDelegate.EndsWith(suffix))
            {
                agentDelegate = agentDelegate.Substring(0, agentDelegate.Length - suffix.Length);
            }

            AgentDelegate = agentDelegate;
            _ignite = ignite;
            _logger = logger;

            _notificationsCache = _ignite.GetCache<AffinityKey, Notification>($"{AgentDelegate}-$notifications");

            var address = $"http://{config.Value.FabricHost}:40400";

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
            _serviceTask.ContinueWith(t =>
            {
                _logger.LogError("Fatal FabricService error: " + t.Exception!.ToString());
            }, TaskContinuationOptions.OnlyOnFaulted);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken token)
        {
            _serviceCancellation.Cancel();
            return _serviceTask ?? Task.CompletedTask;
        }

        private AffinityKey GetAffinityKey(NotificationProto notification)
        {
            return new AffinityKey(
                notification.NotificationKey,
                notification.AffinityCase switch
                {
                    NotificationProto.AffinityOneofCase.StringAffinity => notification.StringAffinity,
                    NotificationProto.AffinityOneofCase.IntAffinity => notification.IntAffinity,
                    _ => null,
                });
        }

        private Channel<(AffinityKey, Notification)> GetChannel(string instance, int? parameter = null)
        {
            var key = (instance, parameter);
            if (_channels.TryGetValue(key, out var channel))
            {
                return channel;
            }
            var newChannel = Channel.CreateUnbounded<(AffinityKey, Notification)>();
            _channels[key] = newChannel;
            return newChannel;
        }

        private async Task RunAsync(CancellationToken cancellationToken = default)
        {
            var client = new Fabric.FabricClient(_grpcChannel);
            using var notifications = client.Notifications(new NotificationFilter { AgentDelegate = AgentDelegate }, null, null, cancellationToken);
            var stream = notifications.ResponseStream;
            while (await stream.MoveNext(cancellationToken))
            {
                var key = GetAffinityKey(stream.Current);
                var notificationResult = await _notificationsCache.TryGetAsync(key);

                if (!notificationResult.Success)
                {
                    _logger.LogDebug($"FabricService failed to read notification: {key}");
                    continue;
                }
                var notification = notificationResult.Value;
                _logger.LogTrace($"FabricService received: {key} {notification}");

                switch (notification)
                {
                    case StreamItemNotification si:
                        await GetChannel(si.Stream, si.Parameter).Writer.WriteAsync((key, notification));
                        break;
                    case StreamTriggerNotification st:
                        await GetChannel(st.Delegate).Writer.WriteAsync((key, notification));
                        break;
                    case CallTriggerNotification ct:
                        await GetChannel(ct.Delegate).Writer.WriteAsync((key, notification));
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

        public async IAsyncEnumerable<(AffinityKey, Notification)> GetNotifications(
            string instance, int? parameter = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogDebug($"FabricService listen on: {instance} {parameter}");
            var reader = GetChannel(instance, parameter).Reader;
            while (true)
            {
                var value = await reader.ReadAsync(cancellationToken);
                _logger.LogDebug($"FabricService sent: {value}");
                yield return value;
            }
        }

        public async Task<(AffinityKey, CallResultNotification)> GetCallNotification(string call, CancellationToken cancellationToken = default)
        {
            var client = new Fabric.FabricClient(_grpcChannel);
            var notification = await client.CallResultNotificationAsync(new CallNotificationFilter
            {
                AgentDelegate = AgentDelegate,
                CallName = call
            });
            var key = GetAffinityKey(notification);
            var fullNotification = await _notificationsCache.GetAsync(key);
            return (key, (CallResultNotification)fullNotification);
        }
    }
}