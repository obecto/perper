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
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "<Pending>")]
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
            : this(fabric, ignite) => StreamName = streamName;
    }

    [PerperData(Name = "PerperStream")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "<Pending>")]
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
            : this(instance, fabric, ignite, serializer, state, logger) => StreamName = streamName;

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
            while (await Task.Run(enumerator.MoveNext).ConfigureAwait(false)) // Blocking, should run in background
            {
                yield return (TResult)_serializer.DeserializeRoot(enumerator.Current!, typeof(TResult))!;
            }
        }

        private class StreamAsyncEnumerable : IAsyncEnumerable<T>
        {
            protected Stream<T> Stream { get; }

            public Dictionary<string, object?> Filter { get; private set; }
            public bool Replay { get; private set; }
            public bool LocalToData { get; private set; }

            public StreamAsyncEnumerable(Stream<T> stream, Dictionary<string, object?> filter, bool replay, bool localToData)
            {
                Stream = stream;
                Replay = replay;
                Filter = filter;
                LocalToData = localToData;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken()) => Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);

            private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                try
                {
                    await AddListenerAsync().ConfigureAwait(false);

                    await foreach (var (key, notification) in Stream._fabric.GetNotifications(Stream._instance.InstanceName, Stream._parameterIndex, cancellationToken))
                    {
                        if (notification is StreamItemNotification si)
                        {
                            var cache = Stream._ignite.GetCache<long, object>(si.Cache).WithKeepBinary<long, object>();

                            object value;
                            try
                            {
                                value = await cache.GetAsync(si.Key).ConfigureAwait(false);
                            }
                            catch (KeyNotFoundException)
                            {
                                Stream._logger.LogError($"Error AffinityKey({key}) Cache Not Found key: {si.Key} in {si.Cache}");
                                continue;
                            }


                            await ((State)Stream._state).LoadStateEntries().ConfigureAwait(false);

                            try
                            {
                                yield return (T)Stream._serializer.DeserializeRoot(value, typeof(T))!;
                            }
                            finally
                            {
                                await ((State)Stream._state).StoreStateEntries().ConfigureAwait(false);

                                await Stream._fabric.ConsumeNotification(key).ConfigureAwait(false);
                            }
                        }
                    }
                }
                finally
                {
                    if (Stream._parameterIndex < 0)
                    {
                        await RemoveListenerAsync().ConfigureAwait(false);
                    }
                }
            }

            private async Task AddListenerAsync()
            {
                var streamsCache = Stream._ignite.GetCache<string, StreamData>("streams");
                var currentValue = await streamsCache.GetAsync(Stream.StreamName).ConfigureAwait(false);

                currentValue.Listeners.Add(new StreamListener
                {
                    AgentDelegate = Stream._fabric.AgentDelegate,
                    Stream = Stream._instance.InstanceName,
                    Parameter = Stream._parameterIndex,
                    Filter = Filter,
                    Replay = Replay,
                    LocalToData = LocalToData,
                });

                await streamsCache.PutAsync(Stream.StreamName, currentValue).ConfigureAwait(false);
            }

            private async Task RemoveListenerAsync()
            {
                var streamsCache = Stream._ignite.GetCache<string, StreamData>("streams");
                var currentValue = await streamsCache.GetAsync(Stream.StreamName).ConfigureAwait(false);

                currentValue.Listeners.RemoveAt(currentValue.Listeners.FindIndex(listener =>
                    listener.Stream == Stream._instance.InstanceName && listener.Parameter == Stream._parameterIndex
                ));

                await streamsCache.PutAsync(Stream.StreamName, currentValue).ConfigureAwait(false);
            }
        }
    }
}