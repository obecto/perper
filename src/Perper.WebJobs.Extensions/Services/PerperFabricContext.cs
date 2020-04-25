using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Client;
using Microsoft.Extensions.Logging;
using Perper.Protocol.Notifications;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricContext : IPerperFabricContext, IAsyncDisposable
    {
        private readonly ILogger<PerperFabricContext> _logger;

        private readonly IIgniteClient _igniteClient;
        
        private readonly Dictionary<string, Dictionary<(NotificationType, string, string), (Type, Channel<Notification>)>> _channels;

        private readonly Dictionary<string, PerperFabricNotifications> _notificationsCache;
        private readonly Dictionary<string, PerperFabricData> _dataCache;

        private Task _listener;
        private CancellationTokenSource _listenerCancellationTokenSource;
        
        public PerperFabricContext(ILogger<PerperFabricContext> logger)
        {
            _logger = logger;

            _igniteClient = Ignition.StartClient(new IgniteClientConfiguration
            {
                Endpoints = new List<string> {"127.0.0.1"}
            });

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
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await socket.ConnectAsync(IPAddress.Loopback, 40400);
                await using var networkStream = new NetworkStream(socket);
                var reader = PipeReader.Create(networkStream);
                try
                {
                    while (true)
                    {
                        var readResult = await reader.ReadAsync(cancellationToken);
                        var buffer = readResult.Buffer;
                        try
                        {
                            if (readResult.IsCanceled)
                            {
                                break;
                            }

                            while (TryParseMessage(ref buffer, out var notification))
                            {
                                await RouteNotification(notification);
                            }

                            if (readResult.IsCompleted)
                            {
                                break;
                            }
                        }
                        finally
                        {
                            reader.AdvanceTo(buffer.Start, buffer.End);
                        }
                    }
                }
                finally
                {
                    await reader.CompleteAsync();
                }
            }, cancellationToken);
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

            result = new PerperFabricData(streamName, _igniteClient, _logger);
            _dataCache[streamName] = result;
            return result;
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
                    notification.ParameterStreamItem = await _igniteClient
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