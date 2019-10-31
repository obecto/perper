using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Ignite.Extensions;
using Perper.Fabric.Streams;

namespace Perper.Fabric.Services
{
    //TODO: Add cancellation tokens
    //TODO: Check binary mode consistency and performance
    [Serializable]
    public class StreamService : IService
    {
        
#pragma warning disable 649
        [InstanceResource] private readonly IIgnite _ignite;
#pragma warning restore 649

        private readonly string _functionName;
        private readonly IEnumerable<Stream> _inputs;
        
        private PipeReader _pipeReader;
        private PipeWriter _pipeWriter;

        public StreamService(string functionName, IEnumerable<Stream> inputs)
        {
            _functionName = functionName;
            _inputs = inputs;
        }

        public void Init(IServiceContext context)
        {
            var clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(IPAddress.Loopback, int.Parse(_functionName)));

            var networkStream = new NetworkStream(clientSocket);
            _pipeReader = PipeReader.Create(networkStream);
            _pipeWriter = PipeWriter.Create(networkStream);
        }

        public void Execute(IServiceContext context)
        {
            Task.WhenAll(_inputs.Select(Engage).Union(new[] {ProcessResult()}));
        }

        public void Cancel(IServiceContext context)
        {
        }

        private async Task Engage(Stream stream)
        {
            await foreach (var items in stream.Listen())
            {
                foreach (var item in items)
                {
                    var data = _ignite.GetBinary().GetBytesFromBinaryObject(item);
                    await _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(data));
                }
            }
        }

        private async Task ProcessResult(CancellationToken cancellationToken = default)
        {
            using var outputStreamer = _ignite.GetDataStreamer<long, IBinaryObject>(_functionName);
         
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _pipeReader.ReadAsync(cancellationToken);
                var item = _ignite.GetBinary().GetBinaryObjectFromBytes(result.Buffer.ToArray());
                await outputStreamer.AddData(DateTime.Now.Millisecond, item);
            }
        }
    }
}