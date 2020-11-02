using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Perper.WebJobs.Extensions.Protocol.Cache;
using Perper.WebJobs.Extensions.Protocol.Notifications;
using Perper.WebJobs.Extensions.Protocol.Protobuf;
using Perper.WebJobs.Extensions.Config;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricContext : IPerperFabricContext, IAsyncDisposable
    {
        private readonly ILogger<PerperFabricContext> _logger;
        private readonly string _igniteHost;

        private IIgniteClient? _igniteClient;

        private readonly Dictionary<string, Dictionary<(NotificationType, string, string), (Type, Channel<Notification>)>> _channels;

        private readonly Dictionary<string, PerperFabricNotifications> _notificationsCache;
        private readonly Dictionary<string, PerperFabricData> _dataCache;

        private Task _listener;
        private CancellationTokenSource _listenerCancellationTokenSource;

        public PerperFabricContext(ILogger<PerperFabricContext> logger, IOptions<PerperFabricConfig> configOptions)
        {
            _logger = logger;
            _igniteHost = configOptions.Value.Host ?? IPAddress.Loopback.ToString();

            _channels = new Dictionary<string, Dictionary<(NotificationType, string, string), (Type, Channel<Notification>)>>();

            _notificationsCache = new Dictionary<string, PerperFabricNotifications>();
            _dataCache = new Dictionary<string, PerperFabricData>();
        }

        public void StartListen(string delegateName)
        {
            _channels.TryAdd(delegateName, new Dictionary<(NotificationType, string, string), (Type, Channel<Notification>)>());

            if (_listener != null) return;

            _listenerCancellationTokenSource = new CancellationTokenSource();

            var cancellationToken = _listenerCancellationTokenSource.Token;
            _listener = Task.Run(async () =>
            {
                var port = 40400;
                using var channel = GrpcChannel.ForAddress($"http://{_igniteHost}:{port}");
                var client = new Fabric.FabricClient(channel);

                var tasks = new List<Task>();

                tasks.Add(Task.Run(async () =>
                {
                    using var streamTriggers = client.StreamTriggers(new StreamTriggerFilter(), null, null, cancellationToken);
                    await foreach (var trigger in streamTriggers.ResponseStream.ReadAllAsync())
                    {
                        await RouteNotification(new Notification
                        {
                            Type = NotificationType.StreamTrigger,
                            Stream = trigger.Stream,
                            Delegate = trigger.Delegate
                        });
                    }
                }));

                tasks.Add(Task.Run(async () =>
                {
                    using var workerTriggers = client.WorkerTriggers(new WorkerTriggerFilter(), null, null, cancellationToken);
                    await foreach (var trigger in workerTriggers.ResponseStream.ReadAllAsync())
                    {
                        await RouteNotification(new Notification
                        {
                            Type = NotificationType.WorkerTrigger,
                            Stream = trigger.Stream,
                            Worker = trigger.Worker,
                            Delegate = trigger.WorkerDelegate
                        });
                    }
                }));

                tasks.Add(Task.Run(async () =>
                {
                    using var streamUpdates = client.StreamUpdates(new StreamUpdateFilter(), null, null, cancellationToken);
                    await foreach (var trigger in streamUpdates.ResponseStream.ReadAllAsync())
                    {
                        if (trigger.ItemUpdate != null)
                        {
                            await RouteNotification(new Notification
                            {
                                Type = NotificationType.StreamParameterItemUpdate,
                                Stream = trigger.Stream,
                                Delegate = trigger.Delegate,
                                Parameter = trigger.ItemUpdate.Parameter,
                                ParameterStream = trigger.ItemUpdate.ParameterStream,
                                ParameterStreamItemKey = trigger.ItemUpdate.ParameterStreamItem
                            });
                        }
                        if (trigger.WorkerResult != null)
                        {
                            await RouteNotification(new Notification
                            {
                                Type = NotificationType.WorkerResult,
                                Stream = trigger.Stream,
                                Delegate = trigger.Delegate,
                                Worker = trigger.WorkerResult.Worker,
                            });
                        }
                    }
                }));

                await Task.WhenAll(tasks);
            });
        }

        public PerperFabricNotifications GetNotifications(string delegateName)
        {
            if (_notificationsCache.TryGetValue(delegateName, out var result)) return result;

            result = new PerperFabricNotifications(delegateName, this);
            _notificationsCache[delegateName] = result;
            return result;
        }

        public PerperFabricData GetData(string streamName)
        {
            if (_dataCache.TryGetValue(streamName, out var result)) return result;

            result = new PerperFabricData(streamName, GetIgniteClient(), _logger);
            _dataCache[streamName] = result;
            return result;
        }

        private IIgniteClient GetIgniteClient()
        {
            _igniteClient ??= Ignition.StartClient(new IgniteClientConfiguration
            {
                Endpoints = new List<string> { _igniteHost },
                BinaryConfiguration = new BinaryConfiguration
                {
                    TypeConfigurations = GetDataTypes().Concat(new[] {
                        typeof(StreamData),
                        typeof(StreamDelegateType),
                        typeof(StreamParam),
                        typeof(WorkerData)
                    }).Select(type => new BinaryTypeConfiguration(type)
                    {
                        Serializer = typeof(IBinarizable).IsAssignableFrom(type) ? null : new BinaryReflectiveSerializer { ForceTimestamp = true },
                        NameMapper = new BinaryBasicNameMapper { IsSimpleName = true }
                    }).ToList()
                }
            });
            return _igniteClient;
        }

        public Channel<Notification> CreateChannel(NotificationType notificationType, string delegateName,
            string streamName = default, string parameterName = default, Type parameterType = default)
        {
            var channel = Channel.CreateUnbounded<Notification>();
            _channels[delegateName][(notificationType, streamName, parameterName)] = (parameterType, channel);
            return channel;
        }

        public async ValueTask DisposeAsync()
        {
            if (_listenerCancellationTokenSource != null)
            {
                try
                {
                    _logger.LogDebug("Disposing context!");
                    _listenerCancellationTokenSource.Cancel();
                    await _listener;
                }
                finally
                {
                    _listenerCancellationTokenSource.Dispose();
                }
            }
        }

        private static IEnumerable<Type> GetDataTypes()
        {
            return
                from assembly in AppDomain.CurrentDomain.GetAssemblies()
                where assembly.GetCustomAttributes<PerperDataAttribute>().Any()
                from type in assembly.GetTypes()
                where type.GetCustomAttributes<PerperDataAttribute>().Any()
                select type;
        }

        private static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out Notification notification)
        {
            if (buffer.TryReadLengthDelimitedMessage(out var messageLength))
            {
                notification = ParseNotification(buffer.Slice(sizeof(ushort), messageLength));
                buffer = buffer.Slice(sizeof(ushort) + messageLength);
                return true;
            }

            notification = new Notification();
            return false;
        }

        private static Notification ParseNotification(ReadOnlySequence<byte> message)
        {
            var reader = new Utf8JsonReader(message);
            var result = JsonSerializer.Deserialize<Notification>(ref reader);
            return result;
        }

        private async ValueTask RouteNotification(Notification notification)
        {
            switch (notification.Type)
            {
                case NotificationType.StreamTrigger:
                    await WriteNotificationToChannel(notification);
                    break;
                case NotificationType.StreamParameterItemUpdate:
                    await WriteNotificationToChannel(notification, notification.Stream,
                        notification.Parameter);
                    break;
                case NotificationType.WorkerTrigger:
                    await WriteNotificationToChannel(notification);
                    break;
                case NotificationType.WorkerResult:
                    await WriteNotificationToChannel(notification, notification.Stream, notification.Worker);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async ValueTask WriteNotificationToChannel(Notification notification, string streamName = default,
            string parameterName = default)
        {
            if (_channels.TryGetValue(notification.Delegate, out var streamChannels))
            {
                Type? parameterType = default;
                if (notification.Type == NotificationType.StreamParameterItemUpdate)
                {
                    notification.ParameterStreamItem = await GetIgniteClient()
                        .GetCache<long, object>(notification.ParameterStream)
                        .GetAsync(notification.ParameterStreamItemKey);
                    parameterType = notification.ParameterStreamItem.GetType();
                }

                if (streamChannels.TryGetValue((notification.Type, streamName, parameterName),
                    out var streamChannelPair))
                {
                    var (expectedType, channel) = streamChannelPair;
                    if (expectedType == default || expectedType.IsAssignableFrom(parameterType))
                    {
                        if (notification.Type == NotificationType.StreamParameterItemUpdate)
                        {
                            _logger.LogTrace("Routed a '{parameterType}' to '{streamName}'s '{parameterName}'",
                                parameterType, streamName, parameterName);
                        }

                        await channel.Writer.WriteAsync(notification, _listenerCancellationTokenSource.Token);
                    }
                    else
                    {
                        if (notification.Type == NotificationType.StreamParameterItemUpdate)
                        {
                            _logger.LogTrace(
                                "Did not route a '{parameterType}' to '{streamName}'s '{parameterName}' due to mismatched types",
                                parameterType, streamName, parameterName);
                        }
                    }
                }
                else
                {
                    _logger.LogTrace(
                        "Did not route a '{parameterType}' to '{streamName}'s '{parameterName}' due to missing listener",
                        parameterType, streamName, parameterName);
                }
            }
        }
    }
}