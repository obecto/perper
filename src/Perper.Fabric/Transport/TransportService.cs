using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Log;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Perper.Protocol.Notifications;

namespace Perper.Fabric.Transport
{
    [Serializable]
    public class TransportService : IService
    {
        [InstanceResource] private IIgnite _ignite;
        
        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        [NonSerialized] private HashSet<Channel<IEnumerable<Notification>>> _channels;

        public void Init(IServiceContext context)
        {
            _channels = new HashSet<Channel<IEnumerable<Notification>>>();
        }

        public void Execute(IServiceContext context)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _task = Task.Run(async () =>
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Any, 40400));
                socket.Listen(120);

                var acceptedListeners = new List<Task>();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var acceptedSocket = await socket.AcceptAsync();
                    acceptedListeners.Add(AcceptSocket(acceptedSocket, cancellationToken));
                }

                await Task.WhenAll(acceptedListeners);
            }, cancellationToken);
        }

        public void Cancel(IServiceContext context)
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        public Task SendAsync(Notification notification)
        {
            return SendAsync(new[] {notification});
        }
        
        public async Task SendAsync(IEnumerable<Notification> notifications)
        {
            await Task.WhenAll(from channel in _channels
                select channel.Writer.WriteAsync(notifications).AsTask());
        }
        

        private async Task AcceptSocket(Socket socket, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<IEnumerable<Notification>>();
            _channels.Add(channel);
            try
            {
                await using var networkStream = new NetworkStream(socket, true);
                var pipeWriter = PipeWriter.Create(networkStream);

                await foreach (var notifications in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    foreach (var notification in notifications)
                    {
                        var message = JsonSerializer.SerializeToUtf8Bytes(notification);
                        var messageBytes = new byte[message.Length + sizeof(ushort)];
                        Array.Copy(BitConverter.GetBytes((ushort) message.Length), messageBytes, sizeof(ushort));
                        Array.Copy(message, 0, messageBytes, sizeof(ushort), message.Length);
                        await pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(messageBytes), cancellationToken);
                    }
                    await pipeWriter.FlushAsync(cancellationToken);
                }
            }
            finally
            {
                _channels.Remove(channel);
            }
        }
    }
}