using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Ignite.Extensions;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricInput
    {
        private readonly PipeReader _reader;
        private readonly IBinary _binary;

        private IBinaryObject _activationObject;

        public PerperFabricInput(Stream stream, IBinary binary)
        {
            _reader = PipeReader.Create(stream);
            _binary = binary;
        }

        public async Task Listen(Func<IBinaryObject, Task> listener, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _reader.ReadAsync(cancellationToken);
                var item = _binary.GetBinaryObjectFromBytes(result.Buffer.ToArray());
                if (_activationObject == null) _activationObject = item;
                await listener(item);
            }
        }

        public IBinaryObject GetActivationObject()
        {
            return _activationObject;
        }
    }
}