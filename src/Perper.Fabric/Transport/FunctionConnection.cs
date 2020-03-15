using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Perper.Fabric.Transport
{
    public class FunctionConnection : IAsyncDisposable
    {
        private readonly Socket _clientSocket;
        private readonly NetworkStream _networkStream;
        private readonly PipeWriter _pipeWriter;

        public FunctionConnection(string delegateName)
        {
            _clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            _clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, 40400));

            _networkStream = new NetworkStream(_clientSocket);
            _pipeWriter = PipeWriter.Create(_networkStream);
        }

        public async ValueTask SendNotificationAsync(object notification)
        {
            var message = notification.ToString();
            var messageBytes = new byte[message.Length + sizeof(ushort)];
            Array.Copy(BitConverter.GetBytes((ushort) message.Length), messageBytes, sizeof(ushort));
            Encoding.ASCII.GetBytes(message, 0, message.Length, messageBytes, sizeof(ushort));
            await _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(messageBytes));
            await _pipeWriter.FlushAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await _networkStream.DisposeAsync();
            _clientSocket.Dispose();
        }
    }
}