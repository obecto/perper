using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Ignite.Extensions;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricOutput
    {
        private readonly PipeWriter _writer;
        private readonly IBinary _binary;
        
        public PerperFabricOutput(NetworkStream networkStream, IBinary binary)
        {
            _writer = PipeWriter.Create(networkStream);
            _binary = binary;
        }

        public async Task AddAsync(IBinaryObject value)
        {
            var data = _binary.GetBytesFromBinaryObject(value);
            await _writer.WriteAsync(data);
        }
    }
}