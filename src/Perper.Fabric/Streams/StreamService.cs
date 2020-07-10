using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
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
        [NonSerialized] private Channel<StreamData> _streamUpdates;
        
        [NonSerialized] private TransportService _transportService;
        
        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        public void Init(IServiceContext context)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            _stream = new Stream(streamsCache[StreamName], _ignite);
            _streamUpdates = Channel.CreateUnbounded<StreamData>();
            
            _transportService = _ignite.GetServices().GetService<TransportService>(nameof(TransportService));
            
        }

        public void Execute(IServiceContext context)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _task = Task.Run(async () =>
            {
                await InvokeAsync();
                await Task.WhenAll(new[]
                {
                    InvokeWorkerAsync(cancellationToken), 
                    UpdateStreamAsync(cancellationToken)
                }.Union(
                    _stream.GetInputStreams().Select(inputStream => EngageAsync(inputStream, cancellationToken))));
            }, cancellationToken);
        }

        public void Cancel(IServiceContext context)
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        public async Task UpdateStreamAsync(StreamData streamData)
        {
            await _streamUpdates.Writer.WriteAsync(streamData);
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
                            Delegate = workerObject.Caller
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

        private async Task UpdateStreamAsync(CancellationToken cancellationToken)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");
            var itemsCache = _ignite.GetOrCreateBinaryCache<long>(_stream.StreamData.Name);
            var outputStreamNames = new HashSet<string>();
            var listenTasks = new List<Task>();

            await foreach (var streamData in _streamUpdates.Reader.ReadAllAsync(cancellationToken))
            {
                if (streamData.LastModified > _stream.StreamData.LastModified)
                {
                    _stream.StreamData.LastModified = streamData.LastModified;
                    await _transportService.SendAsync(new Notification
                    {
                        Type = NotificationType.StreamTrigger,
                        Stream = _stream.StreamData.Name,
                        Delegate = _stream.StreamData.Delegate
                    });
                }
                else if (streamData.StreamParams.TryGetValue("$return", out var names))
                {
                    listenTasks.AddRange(
                        from name in names
                        where outputStreamNames.Add(name)
                        select new Stream(streamsCache[name], _ignite).ListenAsync(cancellationToken).ForEachAsync(
                            async items => { await itemsCache.PutAllAsync(items); }, cancellationToken));
                }
            }
            await Task.WhenAll(listenTasks);
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
                    foreach (var (itemKey, _) in items)
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