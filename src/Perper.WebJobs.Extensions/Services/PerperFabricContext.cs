using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricContext
    {
        private readonly IBinary _binary;

        private readonly Dictionary<string, NetworkStream> _networkStreams;
        private readonly Dictionary<string, PerperFabricInput> _inputs;
        private readonly Dictionary<string, PerperFabricOutput> _outputs;

        public PerperFabricContext(IBinary binary)
        {
            _binary = binary;

            _networkStreams = new Dictionary<string, NetworkStream>();
            _inputs = new Dictionary<string, PerperFabricInput>();
            _outputs = new Dictionary<string, PerperFabricOutput>();
        }

        public async Task<PerperFabricInput> GetInput(string cacheName)
        {
            if (_inputs.TryGetValue(cacheName, out var result)) return result;

            var listenSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            listenSocket.Bind(new UnixDomainSocketEndPoint($"/tmp/{cacheName}.sock"));
            listenSocket.Listen(120);

            var socket = await listenSocket.AcceptAsync();
            var networkStream = new NetworkStream(socket, true);
            _networkStreams[cacheName] = networkStream;

            result = new PerperFabricInput(networkStream, _binary);
            _inputs[cacheName] = result;
            return result;
        }

        public PerperFabricOutput GetOutput(string cacheName)
        {
            if (_outputs.TryGetValue(cacheName, out var result)) return result;

            result = new PerperFabricOutput(_networkStreams[cacheName], _binary);
            _outputs[cacheName] = result;
            return result;
        }

        private class UnixDomainSocketEndPoint : EndPoint
        {
            private readonly string _filename;

            public UnixDomainSocketEndPoint(string filename)
            {
                _filename = filename;
            }

            public override AddressFamily AddressFamily => AddressFamily.Unix;

            public override EndPoint Create(SocketAddress socketAddress)
            {
                var size = socketAddress.Size - 2;
                var bytes = new byte[size];
                for (var i = 0; i < bytes.Length; i++)
                {
                    bytes[i] = socketAddress[i + 2];
                    // There may be junk after the null terminator, so ignore it all.
                    if (bytes[i] == 0)
                    {
                        size = i;
                        break;
                    }
                }

                var name = Encoding.UTF8.GetString(bytes, 0, size);
                return new UnixDomainSocketEndPoint(name);
            }

            public override SocketAddress Serialize()
            {
                var bytes = Encoding.UTF8.GetBytes(_filename);
                var sa = new SocketAddress(AddressFamily, 2 + bytes.Length + 1);
                // sa [0] -> address family low byte, sa [1] -> address family high byte
                for (var i = 0; i < bytes.Length; i++)
                    sa[2 + i] = bytes[i];

                //NULL suffix for non-abstract path
                sa[2 + bytes.Length] = 0;

                return sa;
            }

            public override string ToString()
            {
                return _filename;
            }

            public override int GetHashCode()
            {
                return _filename.GetHashCode();
            }

            public override bool Equals(object o)
            {
                if (!(o is UnixDomainSocketEndPoint other))
                    return false;

                return other._filename == _filename;
            }
        }
    }
}