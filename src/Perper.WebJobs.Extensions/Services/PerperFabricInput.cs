using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Perper.Protocol;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricInput
    {
        private readonly PipeReader _reader;
        private readonly IBinary _binary;

        private IBinaryObject _streamObject;
        private IBinaryObject _workerObject;

        public PerperFabricInput(Stream stream, IBinary binary)
        {
            _reader = PipeReader.Create(stream);
            _binary = binary;
        }

        //TODO: Refactor to Lazy Property
        public IBinaryObject GetStreamObject()
        {
            return _streamObject;
        }

        public async Task<IBinaryObject> GetStreamObjectAsync(CancellationToken cancellationToken)
        {
            if (_streamObject != null)
            {
                return _streamObject;
            }

            var streamObjectBytes = await _reader.ReadAsync(cancellationToken);
            _streamObject = _binary.GetBinaryObjectFromBytes(streamObjectBytes.Buffer.ToArray());
            return _streamObject;
        }

        public async Task<IBinaryObject> GetWorkerObjectAsync(CancellationToken cancellationToken)
        {
            if (_workerObject != null)
            {
                return _workerObject;
            }

            var workerObjectBytes = await _reader.ReadAsync(cancellationToken);
            _workerObject = _binary.GetBinaryObjectFromBytes(workerObjectBytes.Buffer.ToArray());
            return _workerObject;
        }

        public async IAsyncEnumerable<T> GetStream<T>(string parameterName,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _reader.ReadAsync(cancellationToken);
                var item = _binary.GetBinaryObjectFromBytes(result.Buffer.ToArray());
                yield return item.GetField<IBinaryObject>(parameterName).Deserialize<T>();
            }
        }

        public async Task<IBinaryObject> GetWorkerResult()
        {
            var result = await _reader.ReadAsync();
            var item = _binary.GetBinaryObjectFromBytes(result.Buffer.ToArray());
            return item;
        }

        public void UpdateStreamObject(string name, object state)
        {
            var streamObjectBuilder = _streamObject.ToBuilder();
            streamObjectBuilder.SetField(name, state);

            _streamObject = streamObjectBuilder.Build();
        }
    }
}