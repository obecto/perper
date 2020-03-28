using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core.Services;

namespace Perper.Fabric.Transport
{
    [Serializable]
    public class TransportService : IService
    {
        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        [NonSerialized] private HashSet<Channel<object>> _channels;

        public void Init(IServiceContext context)
        {
            _channels = new HashSet<Channel<object>>();
        }

        public void Execute(IServiceContext context)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _task = Task.Run(async () =>
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 40400));
                socket.Listen(120);

                var acceptedListeners = new List<Task>();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var acceptedSocket = await socket.AcceptAsync().WithCancellation(cancellationToken);
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

        public async Task SendAsync(object message)
        {
            await Task.WhenAll(from channel in _channels
                select channel.Writer.WriteAsync(message).AsTask());
        }

        private async Task AcceptSocket(Socket socket, CancellationToken cancellationToken)
        {
            var channel = Channel.CreateUnbounded<object>();
            _channels.Add(channel);
            try
            {
                await using var networkStream = new NetworkStream(socket, true);
                var pipeWriter = PipeWriter.Create(networkStream);
                var jsonWriter = new Utf8JsonWriter(pipeWriter);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var notification = await channel.Reader.ReadAsync(cancellationToken);
                    var message = notification.ToString();
                    var messageBytes = new byte[message.Length + sizeof(ushort)];
                    Array.Copy(BitConverter.GetBytes((ushort) message.Length), messageBytes, sizeof(ushort));
                    Encoding.ASCII.GetBytes(message, 0, message.Length, messageBytes, sizeof(ushort));
                    await pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(messageBytes), cancellationToken);
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