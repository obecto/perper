using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Linq;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Cache.Notifications;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    [PerperData(Name = "PerperStream")]
    public class Stream : IStream
    {
        public string StreamName { get; protected set; }

        [NonSerialized] protected readonly FabricService _fabric;
        [NonSerialized] protected readonly IIgniteClient _ignite;

        [PerperInject]
        protected Stream(FabricService fabric, IIgniteClient ignite)
        {
            _fabric = fabric;
            _ignite = ignite;
        }

        public Stream(string streamName, FabricService fabric, IIgniteClient ignite)
            : this(fabric, ignite)
        {
            StreamName = streamName;
        }
    }

    [PerperData(Name = "PerperStream")]
    public class Stream<T> : Stream, IStream<T>
    {
        [NonSerialized] public string? FunctionName; // HACK: Used for Declare/InitiaizeStream

        [NonSerialized] private readonly PerperInstanceData _instance;
        [NonSerialized] private readonly int _parameterIndex;
        [NonSerialized] private readonly PerperBinarySerializer _serializer;
        [NonSerialized] private readonly IState _state;
        [NonSerialized] private readonly ILogger _logger;

        [PerperInject]
        public Stream(PerperInstanceData instance, FabricService fabric, IIgniteClient ignite, PerperBinarySerializer serializer, IState state, ILogger logger)
            : base(fabric, ignite)
        {
            _instance = instance;
            _parameterIndex = instance.GetStreamParameterIndex();
            _serializer = serializer;
            _state = state;
            _logger = logger;
        }

        public Stream(string streamName, PerperInstanceData instance, FabricService fabric, IIgniteClient ignite, PerperBinarySerializer serializer, IState state, ILogger logger)
            : this(instance, fabric, ignite, serializer, state, logger)
        {
            StreamName = streamName;
        }

        private StreamAsyncEnumerable GetEnumerable(Dictionary<string, object?> filter, bool replay, bool localToData)
        {
            return new StreamAsyncEnumerable(this, filter, replay, localToData, _logger);
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return GetEnumerable(new Dictionary<string, object?>(), false, false).GetAsyncEnumerator(cancellationToken);
        }

        public IAsyncEnumerable<T> DataLocal()
        {
            return GetEnumerable(new Dictionary<string, object?>(), false, true);
        }

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return GetEnumerable(FilterUtils.ConvertFilter(filter), false, dataLocal);
        }

        public IAsyncEnumerable<T> Replay(bool dataLocal = false)
        {
            return GetEnumerable(new Dictionary<string, object?>(), true, dataLocal);
        }

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            return GetEnumerable(FilterUtils.ConvertFilter(filter), true, dataLocal);
        }

        public async IAsyncEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query)
        {
            var cache = _ignite.GetCache<long, T>(StreamName).WithKeepBinary<long, T>(); // NOTE: Will not work with forwarding
            var queryable = query(cache.AsCacheQueryable().Select(pair => pair.Value)).Select(x => (object?)x);

            using var enumerator = queryable.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext)) // Blocking, should run in background
            {
                yield return (TResult)_serializer.DeserializeRoot(enumerator.Current!, typeof(TResult))!;
            }
        }

        public class StreamAsyncEnumerable : IAsyncEnumerable<T>
        {
            protected Stream<T> _stream;

            public Dictionary<string, object?> Filter { get; private set; }
            public bool Replay { get; private set; }
            public bool LocalToData { get; private set; }

            private ILogger _logger;

            public StreamAsyncEnumerable(Stream<T> stream, Dictionary<string, object?> filter, bool replay, bool localToData, ILogger logger)
            {
                _stream = stream;
                Replay = replay;
                Filter = filter;
                LocalToData = localToData;
                _logger = logger;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
            {
                return Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);
            }

            private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                try
                {
                    await AddListenerAsync();

                    await foreach (var (key, notification) in _stream._fabric.GetNotifications(_stream._instance.InstanceName, _stream._parameterIndex, cancellationToken))
                    {
                        if (notification is StreamItemNotification si)
                        {
                            var cache = _stream._ignite.GetCache<long, object>(si.Cache).WithKeepBinary<long, object>();

                            object value;
                            try
                            {
                                value = await cache.GetAsync(si.Key);
                            }
                            catch
                            {
                                _logger.LogError($"Error AffinityKey({key}) Cache Not Found key: {si.Key} in {si.Cache}");
                                continue;
                            }
                            

                            await ((State)_stream._state).LoadStateEntries();

                            try
                            {
                                yield return (T)_stream._serializer.DeserializeRoot(value, typeof(T))!;
                            }
                            finally
                            {
                                await ((State)_stream._state).StoreStateEntries();

                                await _stream._fabric.ConsumeNotification(key);
                            }
                        }
                    }
                }
                finally
                {
                    if (_stream._parameterIndex < 0)
                    {
                        await RemoveListenerAsync();
                    }
                }
            }

            private async Task ModifyStreamDataAsync(Func<IBinaryObject, IBinaryObject> modification)
            {
                var streamsCache = _stream._ignite.GetCache<string, StreamData>("streams").WithKeepBinary<string, IBinaryObject>();
                while (true)
                {
                    var currentValue = await streamsCache.GetAsync(_stream.StreamName);
                    var newValue = modification(currentValue);
                    if (await streamsCache.ReplaceAsync(_stream.StreamName, currentValue, newValue))
                    {
                        break;
                    };
                }
            }

            private Task AddListenerAsync()
            {
                var streamListener = _stream._ignite.GetBinary().ToBinary<object?>(new StreamListener
                {
                    AgentDelegate = _stream._fabric.AgentDelegate,
                    Stream = _stream._instance.InstanceName,
                    Parameter = _stream._parameterIndex,
                    Filter = Filter,
                    Replay = Replay,
                    LocalToData = LocalToData,
                });
                return ModifyStreamDataAsync(streamDataBinary =>
                {
                    var currentListeners = streamDataBinary.GetField<ArrayList>(nameof(StreamData.Listeners));

                    currentListeners.Add(streamListener);

                    var builder = streamDataBinary.ToBuilder();
                    builder.SetField(nameof(StreamData.Listeners), currentListeners);
                    return builder.Build();
                });
            }

            private Task RemoveListenerAsync()
            {
                return ModifyStreamDataAsync(streamDataBinary =>
                {
                    var currentListeners = streamDataBinary.GetField<ArrayList>(nameof(StreamData.Listeners));

                    foreach (var listener in currentListeners)
                    {
                        if (listener is IBinaryObject binObj &&
                            binObj.GetField<string>(nameof(StreamListener.Stream)) == _stream._instance.InstanceName &&
                            binObj.GetField<int>(nameof(StreamListener.Parameter)) == _stream._parameterIndex)
                        {
                            currentListeners.Remove(listener);
                            break;
                        }
                    }

                    var builder = streamDataBinary.ToBuilder();
                    builder.SetField(nameof(StreamData.Listeners), currentListeners);
                    return builder.Build();
                });
            }
        }
    }
}