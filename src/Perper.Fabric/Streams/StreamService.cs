using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Perper.Fabric.Transport;
using Perper.Protocol.Cache;
using Perper.Protocol.Notifications;

namespace Perper.Fabric.Streams
{
    public class StreamService : IService
    {
        private const string ForwardFieldName = "$forward";
        
        [InstanceResource] private IIgnite _ignite;

        [NonSerialized] private IDictionary<string, Stream> _liveStreams;
        [NonSerialized] private IDictionary<string, IList<(string, Stream)>> _liveStreamGraph;
        [NonSerialized] private Channel<(string, long)> _streamItemsUpdates;
        
        [NonSerialized] private TransportService _transportService;
        
        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;
        
        public void Init(IServiceContext context)
        {
            _liveStreams = new Dictionary<string, Stream>();
            _liveStreamGraph = new Dictionary<string, IList<(string, Stream)>>();
            _streamItemsUpdates = Channel.CreateUnbounded<(string, long)>();
            
            _transportService = _ignite.GetServices().GetService<TransportService>(nameof(TransportService));
        }

        public void Execute(IServiceContext context)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            _task = Task.Run(async () => await InvokeStreamItemUpdates(), cancellationToken);
        }

        public void Cancel(IServiceContext context)
        {
            _cancellationTokenSource.Cancel();
            _task.Wait();
        }

        public async Task EngageStreamAsync(Stream stream)
        {
            if (_liveStreams.TryAdd(stream.StreamData.Name, stream))
            {
                await _transportService.SendAsync(new Notification
                {
                    Type = NotificationType.StreamTrigger,
                    Stream = stream.StreamData.Name,
                    Delegate = stream.StreamData.Delegate
                });
            
                var inputStreams = stream.GetInputStreams();
                foreach (var (field, values) in inputStreams)
                {
                    foreach (var value in values)
                    {
                        _liveStreamGraph.TryAdd(value.StreamData.Name, new List<(string, Stream)>());
                        _liveStreamGraph[value.StreamData.Name].Add((field, stream));
                        await value.EngageStreamAsync();
                    }
                }
            }
        }

        public async Task UpdateStreamAsync(Stream stream)
        {
            if(!_liveStreams.ContainsKey(stream.StreamData.Name)) return;

            if (stream.StreamData.LastModified > _liveStreams[stream.StreamData.Name].StreamData.LastModified)
            {
                _liveStreams[stream.StreamData.Name] = stream;
                await _transportService.SendAsync(new Notification
                {
                    Type = NotificationType.StreamTrigger,
                    Stream = stream.StreamData.Name,
                    Delegate = stream.StreamData.Delegate
                });
            }
            else if (stream.StreamData.StreamParams.TryGetValue("$return", out var names))
            {
                foreach (var name in names)
                {
                    _liveStreamGraph.TryAdd(name, new List<(string, Stream)>());
                    if (!_liveStreamGraph[name].Any(tuple =>
                        tuple.Item1 == ForwardFieldName && tuple.Item2.StreamData.Name == stream.StreamData.Name))
                    {
                        _liveStreamGraph[name].Add((ForwardFieldName, stream));    
                    }
                }
                
                var streamsCache = _ignite.GetCache<string, StreamData>("streams");
                await Task.WhenAll(names.Select(name => new Stream(streamsCache[name], _ignite).EngageStreamAsync()));
            }
            else if (stream.StreamData.Workers.Any())
            {
                await InvokeWorker(stream.StreamData);
            }
        }
        
        public void UpdateStreamItemAsync(string stream, long itemKey)
        {
            _streamItemsUpdates.Writer.TryWrite((stream, itemKey));
        }
        
        private async Task InvokeStreamItemUpdates()
        {
            await foreach (var (stream, itemKey) in _streamItemsUpdates.Reader.ReadAllAsync())
            {
                await InvokeStreamItemUpdates(stream, stream, itemKey);
            }
        }

        private async Task InvokeStreamItemUpdates(string targetStream, string stream, long itemKey)
        {
            var streamsToUpdate = _liveStreamGraph[targetStream];
            foreach (var (field, value) in streamsToUpdate)
            {
                if (field == ForwardFieldName)
                {
                    await InvokeStreamItemUpdates(value.StreamData.Name, stream, itemKey);
                }
                else
                {
                    await _transportService.SendAsync(new Notification
                    {
                        Type = NotificationType.StreamParameterItemUpdate,
                        Stream = value.StreamData.Name,
                        Delegate = value.StreamData.Delegate,
                        Parameter = field,
                        ParameterStream = stream,
                        ParameterStreamItemKey = itemKey
                    });
                }
            }
        }
        
        private async Task InvokeWorker(StreamData streamData)
        {
            var workerObject = streamData.Workers.First().Value;
            if (workerObject.Params.HasField("$return"))
            {
                await _transportService.SendAsync(new Notification
                {
                    Type = NotificationType.WorkerResult,
                    Stream = streamData.Name,
                    Worker = workerObject.Name,
                    Delegate = workerObject.Caller
                });
            }
            else
            {
                await _transportService.SendAsync(new Notification
                {
                    Type = NotificationType.WorkerTrigger,
                    Stream = streamData.Name,
                    Worker = workerObject.Name,
                    Delegate = workerObject.Delegate
                });
            }
        }
    }
}