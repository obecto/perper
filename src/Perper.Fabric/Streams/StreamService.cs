using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Event;
using Apache.Ignite.Core.Cache.Query.Continuous;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Perper.Fabric.Utils;
using Perper.Protocol.Notifications;

namespace Perper.Fabric.Streams
{
    [Serializable]
    public class StreamService : IService
    {
        private readonly Stream _stream;

        [InstanceResource] private readonly IIgnite _ignite;

        private PipeWriter _pipeWriter;

        private Task _task;
        private CancellationTokenSource _cancellationTokenSource;

        public StreamService(Stream stream, IIgnite ignite)
        {
            _stream = stream;
            _ignite = ignite;
        }

        public void Init(IServiceContext context)
        {
            var clientSocket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
            clientSocket.Connect(
                new UnixDomainSocketEndPoint($"/tmp/perper_{_stream.StreamObjectTypeName.DelegateName}.sock"));

            var networkStream = new NetworkStream(clientSocket);
            _pipeWriter = PipeWriter.Create(networkStream);
        }

        public void Execute(IServiceContext context)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _task = Task.Run(async () =>
            {
                await Invoke();
                await Task.WhenAll(_stream.GetInputStreams().Select(Engage));
            }, cancellationToken);
        }

        public void Cancel(IServiceContext context)
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        private async ValueTask Invoke()
        {
            await SendNotification(new StreamTriggerNotification());
        }

        private async Task InvokeWorker(CancellationToken cancellationToken)
        {
            var cache = _ignite.GetOrCreateCache<string, IBinaryObject>("workers");
            while (!cancellationToken.IsCancellationRequested)
            {
                var taskCompletionSource = new TaskCompletionSource<IEnumerable<string>>();
                var listener =
                    new ActionListener<string>(events => taskCompletionSource.SetResult(events.Select(e => e.Key)));

                using (cache.QueryContinuous(new ContinuousQuery<string, IBinaryObject>(listener)))
                {
                    if ((await taskCompletionSource.Task).Contains(_stream.StreamObjectTypeName.DelegateName))
                    {
                        await SendNotification(new WorkerTriggerNotification());
                    }
                }
            }
        }

        private async Task Engage(Tuple<string, Stream> inputStream)
        {
            var (parameterName, parameterStream) = inputStream;
            var parameterStreamObjectTypeName = parameterStream.StreamObjectTypeName;
            await foreach (var items in parameterStream.Listen())
            {
                foreach (var item in items)
                {
                    await SendNotification(new StreamParameterItemUpdateNotification(
                        parameterName,
                        parameterStreamObjectTypeName.DelegateType.ToString(),
                        parameterStreamObjectTypeName.DelegateName,
                        item));
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
        }
    }
}