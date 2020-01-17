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
        public string StreamObjectTypeName { get; set; }

        [InstanceResource] private IIgnite _ignite;
        
        [NonSerialized] private Stream _stream;

        [NonSerialized] private FunctionConnection _connection;

        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        public void Init(IServiceContext context)
        {
            _stream = new Stream(StreamBinaryTypeName.Parse(StreamObjectTypeName), _ignite);

            try
            {
                _connection = new FunctionConnection(_stream.StreamObjectTypeName.DelegateName);
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
            await _connection.SendNotificationAsync(new StreamTriggerNotification(_stream.StreamObjectTypeName.StreamName));
        }

        private async Task InvokeWorkerAsync(CancellationToken cancellationToken)
        {
            var streamName = _stream.StreamObjectTypeName.StreamName;
            var workersCache = _ignite.GetOrCreateBinaryCache<string>($"{streamName}_workers");
            await foreach (var workers in workersCache.QueryContinuousAsync(cancellationToken))
            {
                foreach (var (_, workerObject) in workers)
                {
                    var workerTypeName = WorkerBinaryTypeName.Parse(workerObject.GetBinaryType().TypeName);
                    var workerName = workerTypeName.WorkerName;
                    
                    if (workerObject.HasField("$return"))
                    {
                        await _connection.SendNotificationAsync(new WorkerResultSubmitNotification(streamName, workerName));
                    }
                    else
                    {
                        await using var workerConnection = new FunctionConnection(workerTypeName.DelegateName);
                        await workerConnection.SendNotificationAsync(new WorkerTriggerNotification(streamName, workerName));   
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
                        await _connection.SendNotificationAsync(new StreamParameterItemUpdateNotification(streamName, parameterName,
                            itemStreamName, item.GetBinaryType().TypeName, itemKey));
                    }
                }, cancellationToken);
            }));
        }
    }
}