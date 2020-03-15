using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Perper.Fabric.Streams;
using Perper.Fabric.Transport;
using Perper.Protocol.Cache;
using Perper.Protocol.Notifications;

namespace Perper.Fabric.Services
{
    [Serializable]
    public class StreamService : IService
    {
        public string StreamName { get; set; }

        [InstanceResource] private IIgnite _ignite;
        
        [NonSerialized] private Stream _stream;

        [NonSerialized] private FunctionConnection _connection;

        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        public void Init(IServiceContext context)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            _stream = new Stream(streamsCache[StreamName], _ignite);

            try
            {
                _connection = new FunctionConnection(_stream.StreamData.Delegate);
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
            await _connection.SendNotificationAsync(new StreamTriggerNotification(_stream.StreamData.Name){Delegate = _stream.StreamData.Delegate});
        }

        private async Task InvokeWorkerAsync(CancellationToken cancellationToken)
        {
            var streamName = _stream.StreamData.Name;
            var workersCache = _ignite.GetOrCreateCache<string, WorkerData>($"{streamName}_workers");
            await foreach (var workers in workersCache.QueryContinuousAsync(cancellationToken))
            {
                foreach (var (_, workerObject) in workers)
                {
                    if (workerObject.Params.HasField("$return"))
                    {
                        await _connection.SendNotificationAsync(new WorkerResultSubmitNotification(streamName, workerObject.Name){Delegate = _stream.StreamData.Delegate});
                    }
                    else
                    {
                        await using var workerConnection = new FunctionConnection(workerObject.Delegate);
                        await workerConnection.SendNotificationAsync(new WorkerTriggerNotification(streamName, workerObject.Name){Delegate = workerObject.Delegate});   
                    }
                }
            }
        }
        
        private async Task BindOutputAsync(CancellationToken cancellationToken)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            var streamName = _stream.StreamData.Name;
            IEnumerable<Stream> outputStreams = new Stream[] { };
            await foreach (var streams in streamsCache.QueryContinuousAsync(streamName, cancellationToken))
            {
                var (_, streamObject) = streams.Single();
                if (streamObject.Params.HasField("$return"))
                {
                    outputStreams = streamObject.Params.GetField<IBinaryObject[]>("$return").Select(v =>
                        new Stream(streamsCache[v.Deserialize<StreamRef>().StreamName], _ignite));
                    break;
                }
            }

            var itemsCache = _ignite.GetOrCreateBinaryCache<long>(_stream.StreamData.Name);
            await Task.WhenAll(outputStreams.Select(outputStream =>
                outputStream.ListenAsync(cancellationToken).ForEachAsync(async items =>
                    await itemsCache.PutAllAsync(items), cancellationToken)));
        }

        private async Task EngageAsync((string, IEnumerable<Stream>) inputStreams, CancellationToken cancellationToken)
        {
            var streamName = _stream.StreamData.Name;
            var (parameterName, parameterStreams) = inputStreams;
            await Task.WhenAll(parameterStreams.Select(parameterStream =>
            {
                var itemStreamName = parameterStream.StreamData.Name;
                return parameterStream.ListenAsync(cancellationToken).ForEachAsync(async items =>
                {
                    foreach (var (itemKey, item) in items)
                    {
                        await _connection.SendNotificationAsync(new StreamParameterItemUpdateNotification(streamName, parameterName,
                            itemStreamName, item.GetBinaryType().TypeName, itemKey){Delegate = _stream.StreamData.Delegate});
                    }
                }, cancellationToken);
            }));
        }
    }
}