using System.Collections.Generic;
using System.Net.Sockets;
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
            listenSocket.Bind(new UnixDomainSocketEndPoint($"/tmp/perper_{cacheName}.sock"));
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
    }
}