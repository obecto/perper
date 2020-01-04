using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Perper.Protocol.Notifications;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricContext : IPerperFabricContext, IAsyncDisposable
    {
        private readonly ILogger _logger;

        private readonly IIgniteClient _igniteClient;

        private readonly Dictionary<string, Task> _listeners;
        private readonly CancellationTokenSource _listenersCancellationTokenSource;

        private readonly Dictionary<string, Dictionary<(Type, string, string), (Type, object)>> _channels;

        private readonly Dictionary<string, PerperFabricNotifications> _notificationsCache;
        private readonly Dictionary<string, PerperFabricData> _dataCache;

        private readonly Assembly _streamTypesAssembly;

        public PerperFabricContext(IConfiguration configuration, ILogger<PerperFabricContext> logger)
        {
            _logger = logger;

            _igniteClient = Ignition.StartClient(new IgniteClientConfiguration
            {
                Endpoints = new List<string> {"127.0.0.1"}
            });

            _listeners = new Dictionary<string, Task>();
            _listenersCancellationTokenSource = new CancellationTokenSource();

            _channels = new Dictionary<string, Dictionary<(Type, string, string), (Type, object)>>();

            _notificationsCache = new Dictionary<string, PerperFabricNotifications>();
            _dataCache = new Dictionary<string, PerperFabricData>();

            var streamTypesAssemblyName = configuration.GetValue<string>("PerperStreamTypesAssembly");
            if (!string.IsNullOrEmpty(streamTypesAssemblyName))
            {
                _streamTypesAssembly = Assembly.Load(streamTypesAssemblyName);
            }
        }

        public void StartListen(string delegateName)
        {
            if (_listeners.ContainsKey(delegateName)) return;

            var cancellationToken = _listenersCancellationTokenSource.Token;
            _listeners[delegateName] = Task.Run(async () =>
            {
                var socketPath = $"/tmp/perper_{delegateName}.sock";

                using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                socket.Bind(new UnixDomainSocketEndPoint(socketPath));
                socket.Listen(120);
                _logger.LogDebug($"Started listening on socket '{socketPath}'");

                using var acceptedSocket = await socket.AcceptAsync().WithCancellation(cancellationToken);

                await using var networkStream = new NetworkStream(acceptedSocket, true);
                var reader = PipeReader.Create(networkStream);
                while (!cancellationToken.IsCancellationRequested)
                {
                    var buffer = await reader.ReadSequenceAsync(cancellationToken);
                    var messageSize = buffer.Slice(0, 1).ToArray()[0];
                    if (buffer.Length > messageSize)
                    {
                        var message = buffer.Slice(1, messageSize).ToAsciiString();
                        await RouteMessage(delegateName, message);
                        reader.AdvanceTo(buffer.GetPosition(messageSize + 1));
                    }
                    else
                    {
                        reader.AdvanceTo(buffer.Start);
                    }
                }
            }, cancellationToken);
            _channels[delegateName] = new Dictionary<(Type, string, string), (Type, object)>();
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

        public Channel<T> CreateChannel<T>(string delegateName,
            string streamName = default, string parameterName = default, Type parameterType = default)
        {
            var channel = Channel.CreateUnbounded<T>();
            _channels[delegateName][(typeof(T), streamName, parameterName)] = (parameterType, channel);
            return channel;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _logger.LogDebug($"Disposing context!");
                _listenersCancellationTokenSource.Cancel();
                await Task.WhenAll(_listeners.Values);
            }
            finally
            {
                _listenersCancellationTokenSource.Dispose();
            }
        }

        private async ValueTask RouteMessage(string delegateName, string message)
        {
            if (message.StartsWith(nameof(StreamTriggerNotification)))
            {
                await WriteNotificationToChannel(StreamTriggerNotification.Parse(message), delegateName);
            }
            else if (message.StartsWith(nameof(StreamParameterItemUpdateNotification)))
            {
                var notification = StreamParameterItemUpdateNotification.Parse(message);
                await WriteNotificationToChannel(notification, delegateName, notification.StreamName,
                    notification.ParameterName, notification.ItemType);
            }
            else if (message.StartsWith(nameof(WorkerTriggerNotification)))
            {
                await WriteNotificationToChannel(WorkerTriggerNotification.Parse(message), delegateName);
            }
            else if (message.StartsWith(nameof(WorkerResultSubmitNotification)))
            {
                await WriteNotificationToChannel(WorkerResultSubmitNotification.Parse(message), delegateName);
            }
        }

        private async ValueTask WriteNotificationToChannel<T>(T notification, string delegateName,
            string streamName = default, string parameterName = default, string parameterType = default)
        {
            var streamChannels = _channels[delegateName];
            var (expectedType, channel) = streamChannels[(typeof(T), streamName, parameterName)];
            if (expectedType == default || expectedType.IsAssignableFrom(GetParameterType(parameterType)))
            {
                if (typeof(T) == typeof(StreamParameterItemUpdateNotification)) {
                    _logger.LogTrace("Routed a '{parameterType}' to '{streamName}'s '{parameterName}'", parameterType, streamName, parameterName);
                }
                await ((Channel<T>) channel).Writer.WriteAsync(notification, _listenersCancellationTokenSource.Token);
            } else {
                if (typeof(T) == typeof(StreamParameterItemUpdateNotification)) {
                    _logger.LogTrace("Did not route a '{parameterType}' to '{streamName}'s '{parameterName}' due to mismatched types", parameterType, streamName, parameterName);
                }
            }
        }

        private Type GetParameterType(string parameterType)
        {
            return Type.GetType(parameterType, null, (__, t, _) => Type.GetType(t) ?? _streamTypesAssembly.GetType(t));
        }
    }
}