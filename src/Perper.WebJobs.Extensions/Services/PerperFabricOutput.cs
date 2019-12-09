using System.IO;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Perper.Protocol;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricOutput
    {
        private readonly PipeWriter _writer;
        private readonly IBinary _binary;

        public PerperFabricOutput(Stream stream, IBinary binary)
        {
            _writer = PipeWriter.Create(stream);
            _binary = binary;
        }

        public async Task AddAsync(IBinaryObject value)
        {
            var data = _binary.GetBytesFromBinaryObject(value);
            await _writer.WriteAsync(data);
        }
    }
}