using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Client;
using Perper.Protocol.Notifications;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricContext : IPerperFabricContext, IAsyncDisposable
    {
        private readonly IIgniteClient _igniteClient;

        private readonly Dictionary<string, Task> _listeners;
        private readonly CancellationTokenSource _listenersCancellationTokenSource;
        
        private readonly Dictionary<string, Dictionary<(Type, string, string), object>> _channels;
        
        private readonly Dictionary<string, PerperFabricNotifications> _notificationsCache;
        private readonly Dictionary<string, PerperFabricData> _dataCache;

        public PerperFabricContext()
        {
            _igniteClient = Ignition.StartClient(new IgniteClientConfiguration
            {
                Endpoints = new List<string> {"127.0.0.1"}
            });

            _listeners = new Dictionary<string, Task>();
            _listenersCancellationTokenSource = new CancellationTokenSource();
            
            _channels = new Dictionary<string, Dictionary<(Type, string, string), object>>();
            
            _notificationsCache = new Dictionary<string, PerperFabricNotifications>();
            _dataCache = new Dictionary<string, PerperFabricData>();
        }

        public void StartListen(string streamName)
        {
            if (_listeners.ContainsKey(streamName)) return;

            var cancellationToken = _listenersCancellationTokenSource.Token;
            _listeners[streamName] = Task.Run(async () =>
            {
                using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                socket.Bind(new UnixDomainSocketEndPoint($"/tmp/perper_{streamName}.sock"));
                socket.Listen(120);

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
                        await RouteMessage(streamName, message);
                        reader.AdvanceTo(buffer.GetPosition(messageSize + 1));
                    }
                    else
                    {
                        reader.AdvanceTo(buffer.Start);
                    }
                }
            }, cancellationToken);
            _channels[streamName] = new Dictionary<(Type, string, string), object>();
        }

        public PerperFabricNotifications GetNotifications(string streamName)
        {
            if (_notificationsCache.TryGetValue(streamName, out var result)) return result;

            result = new PerperFabricNotifications(streamName, this);
            _notificationsCache[streamName] = result;
            return result;
        }

        public PerperFabricData GetData(string streamName)
        {
            if (_dataCache.TryGetValue(streamName, out var result)) return result;

            result = new PerperFabricData(streamName, _igniteClient);
            _dataCache[streamName] = result;
            return result;
        }

        public Channel<T> CreateChannel<T>(string streamName, string parameterType = default,
            string parameterName = default)
        {
            var result = Channel.CreateUnbounded<T>();
            _channels[streamName][(typeof(T), parameterType, parameterName)] = result;
            return result;
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _listenersCancellationTokenSource.Cancel();
                await Task.WhenAll(_listeners.Values);
            }
            finally
            {
                _listenersCancellationTokenSource.Dispose();
            }
        }

        private async ValueTask RouteMessage(string streamName, string message)
        {
            if (message.StartsWith(nameof(StreamTriggerNotification)))
            {
                await WriteNotificationToChannel(StreamTriggerNotification.Parse(message), streamName);
            }
            else if (message.StartsWith(nameof(StreamParameterItemUpdateNotification)))
            {
                var notification = StreamParameterItemUpdateNotification.Parse(message);
                await WriteNotificationToChannel(notification, streamName, notification.ItemType, notification.ParameterName);
            }
            else if (message.StartsWith(nameof(WorkerTriggerNotification)))
            {
                await WriteNotificationToChannel(WorkerTriggerNotification.Parse(message), streamName);
            }
            else if (message.StartsWith(nameof(WorkerResultSubmitNotification)))
            {
                await WriteNotificationToChannel(WorkerResultSubmitNotification.Parse(message), streamName);
            }
        }

        private async ValueTask WriteNotificationToChannel<T>(T notification, string streamName,
            string parameterType = default, string parameterName = default)
        {
            var streamChannels = _channels[streamName];
            if (streamChannels.TryGetValue((typeof(T), parameterType, parameterName), out var channel))
            {
                await ((Channel<T>) channel).Writer.WriteAsync(notification, _listenersCancellationTokenSource.Token);
            }
        }
    }
}