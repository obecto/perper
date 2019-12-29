using System;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Perper.Fabric.Streams;
using Perper.Protocol.Cache;
using Perper.Protocol.Notifications;

namespace Perper.Fabric.Services
{
    [Serializable]
    public class StreamService : IService
    {
        public string StreamObjectTypeName { get; set; }

        [InstanceResource] private IIgnite _ignite;
        
        [NonSerialized] private Stream _stream;

        [NonSerialized] private PipeWriter _pipeWriter;

        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        public void Init(IServiceContext context)
        {
            _stream = new Stream(StreamBinaryTypeName.Parse(StreamObjectTypeName), _ignite);

            try
            {
                var clientSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                clientSocket.Connect(
                    new UnixDomainSocketEndPoint($"/tmp/perper_{_stream.StreamObjectTypeName.DelegateName}.sock"));

                var networkStream = new NetworkStream(clientSocket);
                _pipeWriter = PipeWriter.Create(networkStream);

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public void Execute(IServiceContext context)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _task = Task.Run(async () =>
            {
                await Invoke();
                await Task.WhenAll(new[] {InvokeWorker(cancellationToken)}.Union(_stream.GetInputStreams()
                    .Select(inputStream => Engage(inputStream, cancellationToken))));
            }, cancellationToken);
        }

        public void Cancel(IServiceContext context)
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        private async ValueTask Invoke()
        {
            await SendNotification(new StreamTriggerNotification(_stream.StreamObjectTypeName.StreamName));
        }

        private async Task InvokeWorker(CancellationToken cancellationToken)
        {
            var cache = _ignite.GetOrCreateBinaryCache<string>("workers");
            var streamName = _stream.StreamObjectTypeName.StreamName;
            await foreach (var workers in cache.QueryContinuousAsync(streamName, cancellationToken))
            {
                foreach (var (_, worker) in workers)
                {
                    if (worker.HasField("$return"))
                    {
                        await SendNotification(new WorkerResultSubmitNotification(streamName));
                    }
                    else
                    {
                        await SendNotification(new WorkerTriggerNotification(streamName));    
                    }
                }
            }
        }

        private async Task Engage(Tuple<string, Stream> inputStream, CancellationToken cancellationToken)
        {
            var streamName = _stream.StreamObjectTypeName.StreamName;
            var (parameterName, parameterStream) = inputStream;
            var itemStreamName = parameterStream.StreamObjectTypeName.StreamName;
            await foreach (var items in parameterStream.ListenAsync(cancellationToken))
            {
                foreach (var (itemKey, item) in items)
                {
                    await SendNotification(new StreamParameterItemUpdateNotification(streamName, parameterName,
                        itemStreamName, item.GetBinaryType().TypeName, itemKey));
                }
            }
        }

        private async ValueTask SendNotification(object notification)
        {
            var message = notification.ToString();
            var messageBytes = new byte[message.Length + 1];
            messageBytes[0] = (byte) message.Length;
            Encoding.Default.GetBytes(message, 0, message.Length, messageBytes, 1);
            await _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(messageBytes));
            await _pipeWriter.FlushAsync();
        }
    }
}