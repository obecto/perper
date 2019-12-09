using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Perper.Protocol;
using Perper.Protocol.Header;

namespace Perper.Fabric.Streams
{
    //TODO: Add cancellation tokens
    //TODO: Check binary mode consistency and performance
    [Serializable]
    public class StreamService : IService
    {
        private readonly Stream _stream;

        [InstanceResource] private readonly IIgnite _ignite;

        private PipeReader _pipeReader;
        private PipeWriter _pipeWriter;

        public StreamService(Stream stream, IIgnite ignite)
        {
            _stream = stream;
            _ignite = ignite;
        }

        public void Init(IServiceContext context)
        {
            var clientSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            clientSocket.Connect(new UnixDomainSocketEndPoint($"/tmp/perper_{_stream.StreamHeader.Name}.sock"));

            var networkStream = new NetworkStream(clientSocket);
            _pipeReader = PipeReader.Create(networkStream);
            _pipeWriter = PipeWriter.Create(networkStream);
        }

        public void Execute(IServiceContext context)
        {
            Task.WhenAll(new[] {Invoke(), ProcessResult()}.Union(_stream.GetInputStreams().Select(Engage)));
        }

        public void Cancel(IServiceContext context)
        {
        }

        private async Task Invoke()
        {
            var data = _ignite.GetBinary().GetBytesFromBinaryObject(_stream.StreamObject);
            await _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(data));
        }


        private async Task Engage(Tuple<string, Stream> inputStream)
        {
            var (parameterName, stream) = inputStream;
            await foreach (var items in stream.Listen())
            {
                foreach (var item in items)
                {
                    var builder = _ignite.GetBinary().GetBuilder(_stream.StreamObject.GetBinaryType().TypeName);
                    builder.SetField(parameterName, item);
                    var data = _ignite.GetBinary().GetBytesFromBinaryObject(builder.Build());
                    await _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(data));
                }
            }
        }

        private async Task ProcessResult(CancellationToken cancellationToken = default)
        {
            using var outputStreamer = _ignite.GetDataStreamer<long, IBinaryObject>(_stream.StreamHeader.Name);

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _pipeReader.ReadAsync(cancellationToken);
                var item = _ignite.GetBinary().GetBinaryObjectFromBytes(result.Buffer.ToArray());
                if (item.GetBinaryType().TypeName.StartsWith(nameof(StreamHeader)))
                {
                    var childStream = _stream.CreateChildStream(item);
                    if (childStream.StreamHeader.Kind == StreamKind.Pipe) continue;
                    await foreach (var unused in childStream.Listen(cancellationToken))
                    {
                        //TODO: Shouldn't enter, consider throwing an exception
                    }
                }
                else
                {
                    await outputStreamer.AddData(DateTime.Now.Millisecond, item);
                }
            }
        }
    }
}