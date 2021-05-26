using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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

#pragma warning disable CS8618
        [PerperInject]
        protected Stream(FabricService fabric, IIgniteClient ignite)
        {
            _fabric = fabric;
            _ignite = ignite;
        }
#pragma warning restore CS8618

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

        private StreamAsyncEnumerable GetEnumerable(Dictionary<string, object?> filter, bool replay, bool localToData) => new StreamAsyncEnumerable(this, filter, replay, localToData);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => GetEnumerable(new Dictionary<string, object?>(), false, false).GetAsyncEnumerator(cancellationToken);

        public IAsyncEnumerable<T> DataLocal() => GetEnumerable(new Dictionary<string, object?>(), false, true);

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false) => GetEnumerable(FilterUtils.ConvertFilter(filter), false, dataLocal);

        public IAsyncEnumerable<T> Replay(bool dataLocal = false) => GetEnumerable(new Dictionary<string, object?>(), true, dataLocal);

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false) => GetEnumerable(FilterUtils.ConvertFilter(filter), true, dataLocal);

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

            public StreamAsyncEnumerable(Stream<T> stream, Dictionary<string, object?> filter, bool replay, bool localToData)
            {
                _stream = stream;
                Replay = replay;
                Filter = filter;
                LocalToData = localToData;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) => Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);

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
                                _stream._logger.LogError($"Error AffinityKey({key}) Cache Not Found key: {si.Key} in {si.Cache}");
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

            private async Task AddListenerAsync()
            {
                var streamsCache = _stream._ignite.GetCache<string, StreamData>("streams");
                var currentValue = await streamsCache.GetAsync(_stream.StreamName);

                currentValue.Listeners.Add(new StreamListener
                {
                    AgentDelegate = _stream._fabric.AgentDelegate,
                    Stream = _stream._instance.InstanceName,
                    Parameter = _stream._parameterIndex,
                    Filter = Filter,
                    Replay = Replay,
                    LocalToData = LocalToData,
                });

                await streamsCache.PutAsync(_stream.StreamName, currentValue);
            }

            private async Task RemoveListenerAsync()
            {
                var streamsCache = _stream._ignite.GetCache<string, StreamData>("streams");
                var currentValue = await streamsCache.GetAsync(_stream.StreamName);

                currentValue.Listeners.RemoveAt(currentValue.Listeners.FindIndex(listener =>
                    listener.Stream == _stream._instance.InstanceName && listener.Parameter == _stream._parameterIndex
                ));

                await streamsCache.PutAsync(_stream.StreamName, currentValue);
            }
        }
    }
}