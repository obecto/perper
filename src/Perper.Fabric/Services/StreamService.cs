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
                await InvokeAsync();
                await Task.WhenAll(new[] {InvokeWorkerAsync(cancellationToken), BindOutputAsync(cancellationToken)}.Union(
                    _stream.GetInputStreams().Select(inputStream => EngageAsync(inputStream, cancellationToken))));
            }, cancellationToken);
        }

        public void Cancel(IServiceContext context)
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        private async ValueTask InvokeAsync()
        {
            await SendNotificationAsync(new StreamTriggerNotification(_stream.StreamObjectTypeName.StreamName));
        }

        private async Task InvokeWorkerAsync(CancellationToken cancellationToken)
        {
            var cache = _ignite.GetOrCreateBinaryCache<string>("workers");
            var streamName = _stream.StreamObjectTypeName.StreamName;
            await foreach (var workers in cache.QueryContinuousAsync(streamName, cancellationToken))
            {
                foreach (var (_, workerObject) in workers)
                {
                    if (workerObject.HasField("$return"))
                    {
                        await SendNotificationAsync(new WorkerResultSubmitNotification(streamName));
                    }
                    else
                    {
                        await SendNotificationAsync(new WorkerTriggerNotification(streamName));    
                    }
                }
            }
        }

        private async Task BindOutputAsync(CancellationToken cancellationToken)
        {
            var cache = _ignite.GetOrCreateBinaryCache<string>("streams");
            var streamName = _stream.StreamObjectTypeName.StreamName;
            IEnumerable<Stream> outputStreams = new Stream[] { };
            await foreach (var streams in cache.QueryContinuousAsync(streamName, cancellationToken))
            {
                var (_, streamObject) = streams.Single();
                if (streamObject.HasField("$return"))
                {
                    outputStreams = streamObject.GetField<IBinaryObject[]>("$return").Select(v =>
                        new Stream(StreamBinaryTypeName.Parse(v.GetBinaryType().TypeName), _ignite));
                    break;
                }
            }

            var itemsCache = _ignite.GetOrCreateBinaryCache<long>(_stream.StreamObjectTypeName.StreamName);
            await Task.WhenAll(outputStreams.Select(outputStream =>
                outputStream.ListenAsync(cancellationToken).ForEachAsync(async items =>
                    await itemsCache.PutAllAsync(items), cancellationToken)));
        }

        private async Task EngageAsync((string, IEnumerable<Stream>) inputStreams, CancellationToken cancellationToken)
        {
            var streamName = _stream.StreamObjectTypeName.StreamName;
            var (parameterName, parameterStreams) = inputStreams;
            await Task.WhenAll(parameterStreams.Select(parameterStream =>
            {
                var itemStreamName = parameterStream.StreamObjectTypeName.StreamName;
                return parameterStream.ListenAsync(cancellationToken).ForEachAsync(async items =>
                {
                    foreach (var (itemKey, item) in items)
                    {
                        await SendNotificationAsync(new StreamParameterItemUpdateNotification(streamName, parameterName,
                            itemStreamName, item.GetBinaryType().TypeName, itemKey));
                    }
                }, cancellationToken);
            }));
        }

        private async ValueTask SendNotificationAsync(object notification)
        {
            var message = notification.ToString();
            var messageBytes = new byte[message.Length + sizeof(ushort)];
            Array.Copy(BitConverter.GetBytes((ushort)message.Length), messageBytes, sizeof(ushort));
            Encoding.ASCII.GetBytes(message, 0, message.Length, messageBytes, sizeof(ushort));
            Console.WriteLine($"Sending Notification:{message}");
            await _pipeWriter.WriteAsync(new ReadOnlyMemory<byte>(messageBytes));
            await _pipeWriter.FlushAsync();
        }
    }
}