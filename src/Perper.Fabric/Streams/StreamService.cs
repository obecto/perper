using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Resource;
using Apache.Ignite.Core.Services;
using Apache.Ignite.Core.Binary;
using Perper.Fabric.Transport;
using Perper.Protocol.Cache;
using Perper.Protocol.Notifications;

namespace Perper.Fabric.Streams
{
    public class StreamService : IService
    {
        private const string ForwardFieldName = "$forward";

        private class Ref<T>
        {
            public T Value;
            public Ref(T value)
            {
                Value = value;
            }
        }

        [InstanceResource] private IIgnite _ignite;

        [NonSerialized] private IDictionary<string, Stream> _liveStreams;
        [NonSerialized] private IDictionary<string, IList<(string, string?, object?, Stream)>> _liveStreamGraph;
        [NonSerialized] private Channel<(string, long)> _streamItemsUpdates;

        [NonSerialized] private HashSet<string> _liveWorkers;

        [NonSerialized] private TransportService _transportService;

        [NonSerialized] private Task _task;
        [NonSerialized] private CancellationTokenSource _cancellationTokenSource;

        public void Init(IServiceContext context)
        {
            _liveStreams = new Dictionary<string, Stream>();
            _liveStreamGraph = new Dictionary<string, IList<(string, string?, object?, Stream)>>();
            _streamItemsUpdates = Channel.CreateUnbounded<(string, long)>();

            _liveWorkers = new HashSet<string>();

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
                    foreach (var (value, filterField, filterValue) in values)
                    {
                        _liveStreamGraph.TryAdd(value.StreamData.Name, new List<(string, string?, object?, Stream)>());
                        _liveStreamGraph[value.StreamData.Name].Add((field, filterField, filterValue, stream));
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
                foreach (var (name, filterField, filterValue) in names)
                {
                    _liveStreamGraph.TryAdd(name, new List<(string, string?, object?, Stream)>());
                    if (!_liveStreamGraph[name].Any(tuple =>
                        tuple.Item1 == ForwardFieldName && tuple.Item4.StreamData.Name == stream.StreamData.Name))
                    {
                        _liveStreamGraph[name].Add((ForwardFieldName, filterField, filterValue, stream));
                    }
                }

                var streamsCache = _ignite.GetCache<string, StreamData>("streams");
                await Task.WhenAll(names.Select(name => new Stream(streamsCache[name.Item1], _ignite).EngageStreamAsync()));
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
                await InvokeStreamItemUpdates(stream, stream, itemKey, new Ref<object?>(null));
            }
        }

        private async Task InvokeStreamItemUpdates(string targetStream, string stream, long itemKey, Ref<object?> itemValue)
        {
            var streamsToUpdate = _liveStreamGraph[targetStream];
            foreach (var (field, filterField, filterValue, value) in streamsToUpdate)
            {
                if (filterField != null && filterValue != null)
                {
                    if (itemValue.Value == null)
                    {
                        itemValue.Value = _ignite.GetBinaryCache<long>(stream)[itemKey];
                    }
                    if (itemValue.Value is IBinaryObject binaryObject)
                    {
                        if (!filterValue.Equals(binaryObject.GetField<object>(filterField)))
                        {
                            continue;
                        }
                    }
                }

                if (field == ForwardFieldName)
                {
                    await InvokeStreamItemUpdates(value.StreamData.Name, stream, itemKey, itemValue);
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
            foreach (var workerObject in streamData.Workers.Values)
            {
                if (workerObject.Params.HasField("$return") && _liveWorkers.Contains(workerObject.Name))
                {
                    _liveWorkers.Remove(workerObject.Name);
                    await _transportService.SendAsync(new Notification
                    {
                        Type = NotificationType.WorkerResult,
                        Stream = streamData.Name,
                        Worker = workerObject.Name,
                        Delegate = workerObject.Caller
                    });
                }
                else if (!workerObject.Params.HasField("$return") && !_liveWorkers.Contains(workerObject.Name))
                {
                    _liveWorkers.Add(workerObject.Name);
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
}