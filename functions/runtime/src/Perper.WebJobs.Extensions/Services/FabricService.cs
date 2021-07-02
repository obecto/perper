using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
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
using Perper.WebJobs.Extensions.Cache;
using Notification = Perper.WebJobs.Extensions.Cache.Notifications.Notification;
using NotificationProto = Perper.Protocol.Protobuf.Notification;
using Perper.Protocol.Protobuf;

namespace Perper.WebJobs.Extensions.Services
{
    public class FabricService : IHostedService
    {
        public string AgentDelegate { get; }
        private bool isInitialAgent;

        private readonly IIgniteClient _ignite;
        private ILogger _logger;

        private GrpcChannel _grpcChannel;
        private readonly ICacheClient<NotificationKey, Notification> _notificationsCache;
        private readonly ConcurrentDictionary<(string, int?), Channel<(NotificationKey, Notification)>> _channels = new ConcurrentDictionary<(string, int?), Channel<(NotificationKey, Notification)>>();

        private CancellationTokenSource _serviceCancellation = new CancellationTokenSource();
        private Task? _serviceTask;

        public FabricService(IIgniteClient ignite, IOptions<PerperConfig> config, ILogger<FabricService> logger)
        {
            AgentDelegate = Environment.GetEnvironmentVariable("PERPER_AGENT_NAME") ?? GetAgentDelegateFromPath();
            isInitialAgent = (Environment.GetEnvironmentVariable("PERPER_ROOT_AGENT") ?? "") == AgentDelegate;

            _ignite = ignite;
            _logger = logger;

            _notificationsCache = _ignite.GetCache<NotificationKey, Notification>($"{AgentDelegate}-$notifications");

#if NETSTANDARD2_0
            _grpcChannel = new GrpcChannel(config.Value.FabricHost, config.Value.FabricGrpcPort, ChannelCredentials.Insecure);
#else
            var address = $"http://{config.Value.FabricHost}:{config.Value.FabricGrpcPort}";
            _grpcChannel = GrpcChannel.ForAddress(address);
#endif
        }

        private string GetAgentDelegateFromPath()
        {
            var projectRootPath = Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory()));
            var agentDelegate = Path.GetFileName(projectRootPath)!;
            if (agentDelegate == "src")
            {
                agentDelegate = Path.GetFileName(Path.GetDirectoryName(projectRootPath))!;
            }
            var suffix = ".FunctionApp";
            if (agentDelegate.EndsWith(suffix))
            {
                agentDelegate = agentDelegate.Substring(0, agentDelegate.Length - suffix.Length);
            }
            // NOTE: Currently this can still result in agent delegates with dots, dashes, or underscores,
            // which is bad as it is currently impossible to have a function named the same as such an agent
            return agentDelegate;
        }

        public Task StartAsync(CancellationToken token)
        {
            _logger.LogInformation("Started FabricService for agent '" + AgentDelegate + "'");
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

        private NotificationKey GetNotificationKey(NotificationProto notification)
        {
            return (notification.AffinityCase switch
            {
                NotificationProto.AffinityOneofCase.StringAffinity => new NotificationKeyString(
                    notification.NotificationKey,
                    notification.StringAffinity),
                NotificationProto.AffinityOneofCase.IntAffinity => new NotificationKeyLong(
                    notification.NotificationKey,
                    notification.IntAffinity),
                _ => default!
            })!;
        }

        private Channel<(NotificationKey, Notification)> GetChannel(string instance, int? parameter = null) =>
            _channels.GetOrAdd((instance, parameter), _ =>
                Channel.CreateUnbounded<(NotificationKey, Notification)>());

        private async Task StartInitialAgent(CancellationToken cancellationToken = default)
        {
            if (isInitialAgent)
            {
                _logger.LogInformation("Calling initial agent");
                var callDelegate = AgentDelegate;
                var agentName = AgentDelegate + "-$launchAgent";
                var callName = callDelegate + "-$launchCall";

                var callsCache = _ignite.GetCache<string, CallData>("calls");
                await callsCache.PutAsync(callName, new CallData
                {
                    Agent = agentName,
                    AgentDelegate = AgentDelegate,
                    Delegate = callDelegate,
                    CallerAgentDelegate = "",
                    Caller = "",
                    Finished = false,
                    LocalToData = false
                });
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _ = StartInitialAgent();

            var client = new Fabric.FabricClient(_grpcChannel);
            using var notifications = client.Notifications(new NotificationFilter { Agent = AgentDelegate }, null, null, cancellationToken);
            var stream = notifications.ResponseStream;
            while (await stream.MoveNext(cancellationToken))
            {
                var key = GetNotificationKey(stream.Current);

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

        public Task ConsumeNotification(NotificationKey key, CancellationToken cancellationToken = default)
        {
            return _notificationsCache.RemoveAsync(key);
        }

        public async IAsyncEnumerable<(NotificationKey, Notification)> GetNotifications(
            string instance, int? parameter = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _logger.LogTrace($"FabricService listen on: {instance} {parameter}");
            var reader = GetChannel(instance, parameter).Reader;
            while (true)
            {
                var value = await reader.ReadAsync(cancellationToken);
                _logger.LogTrace($"FabricService sent: {value}");
                yield return value;
            }
        }

        public async Task<(NotificationKey, CallResultNotification)> GetCallNotification(string call, CancellationToken cancellationToken = default)
        {
            var client = new Fabric.FabricClient(_grpcChannel);
            var notification = await client.CallResultNotificationAsync(new CallNotificationFilter
            {
                Agent = AgentDelegate,
                Call = call
            });
            var key = GetNotificationKey(notification);

            var fullNotification = await _notificationsCache.GetAsync(key);
            return (key, (CallResultNotification)fullNotification);
        }
    }
}