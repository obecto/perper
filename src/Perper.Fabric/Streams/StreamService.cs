using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Perper.Fabric.Transport;
using Perper.Protocol.Cache;
using Perper.Protocol.Notifications;

namespace Perper.Fabric.Streams
{
    [Serializable]
    public class StreamService : IService
    {
        public string StreamName { get; set; }

        [InstanceResource] private IIgnite _ignite;

        [NonSerialized] private Stream _stream;

        [NonSerialized] private TransportService _transportService;

        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        public void Init(IServiceContext context)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            _stream = new Stream(streamsCache[StreamName], _ignite);

            _transportService = _ignite.GetServices().GetService<TransportService>(nameof(TransportService));
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
            await _transportService.SendAsync(new Notification
            {
                Type = NotificationType.StreamTrigger,
                Stream = _stream.StreamData.Name,
                Delegate = _stream.StreamData.Delegate
            });
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
                        await _transportService.SendAsync(new Notification
                        {
                            Type = NotificationType.WorkerResult,
                            Stream = streamName,
                            Worker = workerObject.Name,
                            Delegate = _stream.StreamData.Delegate
                        });
                    }
                    else
                    {
                        await _transportService.SendAsync(new Notification
                        {
                            Type = NotificationType.WorkerTrigger,
                            Stream = streamName,
                            Worker = workerObject.Name,
                            Delegate = workerObject.Delegate
                        });
                    }
                }
            }
        }

        private async Task BindOutputAsync(CancellationToken cancellationToken)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            var streamName = _stream.StreamData.Name;
            var itemsCache = _ignite.GetOrCreateBinaryCache<long>(streamName);
            var outputStreams = new Dictionary<string, Stream>();
            var deployments = new List<IAsyncDisposable>();
            var listenTasks = new List<Task>();
            try
            {
                await foreach (var streams in streamsCache.QueryContinuousAsync(streamName, cancellationToken))
                {
                    var (_, streamObject) = streams.Single();
                    if (streamObject.Params.HasField("$return"))
                    {
                        var newOutputStreams = new List<Stream>();
                        foreach (var outputStreamRef in streamObject.Params.GetField<IBinaryObject[]>("$return"))
                        {
                            var outputStreamName = outputStreamRef.Deserialize<StreamRef>().StreamName;
                            var outputStream = new Stream(streamsCache[outputStreamName], _ignite);
                         
                            if (outputStreams.TryAdd(outputStreamName, outputStream))
                            {
                                var outputStreamServiceDeployment = new StreamServiceDeployment(outputStreamName, _ignite);
                                await outputStreamServiceDeployment.DeployAsync();
                            
                                deployments.Add(outputStreamServiceDeployment);
                                newOutputStreams.Add(outputStream);
                            }
                        }

                        listenTasks.AddRange(newOutputStreams.Select(stream =>
                        {
                            return stream.ListenAsync(cancellationToken).ForEachAsync(
                                async items =>
                                {
                                    await itemsCache.PutAllAsync(items);
                                }, cancellationToken);
                        }));
                    }
                }
                await Task.WhenAll(listenTasks);
            }
            finally
            {
                await Task.WhenAll(deployments.Select(d => d.DisposeAsync().AsTask()));
            }
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
                        await _transportService.SendAsync(new Notification
                        {
                            Type = NotificationType.StreamParameterItemUpdate,
                            Stream = streamName,
                            Delegate = _stream.StreamData.Delegate,
                            Parameter = parameterName,
                            ParameterStream = itemStreamName,
                            ParameterStreamItemKey = itemKey
                        });
                    }
                }, cancellationToken);
            }));
        }
    }
}