using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Cache.Notifications;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class Stream : IStream
    {
        public string StreamName { get; protected set; }

        [PerperInject] protected readonly FabricService _fabric;
        [PerperInject] protected readonly IIgniteClient _ignite;

        public Stream(string streamName, FabricService fabric, IIgniteClient ignite)
        {
            StreamName = streamName;
            _fabric = fabric;
            _ignite = ignite;
        }
    }

    public class Stream<T> : Stream, IStream<T>
    {
        [NonSerialized] public string? FunctionName; // HACK: Used for Declare/InitiaizeStream

        [NonSerialized] private PerperInstanceData _instance;
        [NonSerialized] private int _parameterIndex;
        [PerperInject]
        protected PerperInstanceData? Instance
        { // Used so that parameter index is updated on deserialization from parameters
            set
            {
                if (value != null)
                {
                    _instance = value;
                    _parameterIndex = value.GetStreamParameterIndex();
                }
            }
        }

        [PerperInject] protected readonly IState _state;

#pragma warning disable 8618 // _instance and _parameterIndex come from Instance
        public Stream(string streamName, PerperInstanceData instance, FabricService fabric, IIgniteClient ignite, IState state)
            : base(streamName, fabric, ignite)
        {
            Instance = instance;
            _state = state;
        }
#pragma warning restore 8618

        private StreamAsyncEnumerable GetEnumerable(Dictionary<string, object> filter, bool localToData)
        {
            return new StreamAsyncEnumerable(this, filter, localToData);
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return GetEnumerable(new Dictionary<string, object>(), false).GetAsyncEnumerator(cancellationToken);
        }

        public IAsyncEnumerable<T> DataLocal()
        {
            return GetEnumerable(new Dictionary<string, object>(), true);
        }

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Replay(bool dataLocal = false)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Replay(Func<IQueryable<T>, IQueryable<T>> query, bool dataLocal = false)
        {
            throw new NotImplementedException();
        }

        public class StreamAsyncEnumerable : IAsyncEnumerable<T>
        {
            protected Stream<T> _stream;

            public Dictionary<string, object> Filter { get; private set; }

            public bool LocalToData { get; private set; }

            public StreamAsyncEnumerable(Stream<T> stream, Dictionary<string, object> filter, bool localToData)
            {
                Filter = filter;
                LocalToData = localToData;
                _stream = stream;
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
                            var cache = _stream._ignite.GetCache<long, T>(si.Cache);
                            var value = await cache.GetAsync(si.Key);

                            await ((State)_stream._state).LoadStateEntries();

                            yield return value;

                            await ((State)_stream._state).StoreStateEntries();

                            await _stream._fabric.ConsumeNotification(key);
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

            private async Task ModifyStreamDataAsync(Action<StreamData> modification)
            {
                var streamsCache = _stream._ignite.GetCache<string, StreamData>("streams").WithKeepBinary<string, IBinaryObject>();
                while (true)
                {
                    var currentValue = await streamsCache.GetAsync(_stream.StreamName);
                    var newValue = currentValue.Deserialize<StreamData>();
                    modification(newValue);
                    var newValueBinary = _stream._ignite.GetBinary().ToBinary<IBinaryObject>(newValue);
                    if (await streamsCache.ReplaceAsync(_stream.StreamName, currentValue, newValueBinary))
                    {
                        break;
                    };
                }
            }

            private Task AddListenerAsync()
            {
                var streamListener = new StreamListener
                {
                    AgentDelegate = _stream._fabric.AgentDelegate,
                    Stream = _stream._instance.InstanceName,
                    Parameter = _stream._parameterIndex,
                    Filter = Filter,
                    LocalToData = LocalToData,
                };
                return ModifyStreamDataAsync(streamData =>
                {
                    streamData.Listeners.Add(streamListener);
                });
            }

            private Task RemoveListenerAsync()
            {
                // If listener is anonymous (_parameter is null) then remove the listener
                return ModifyStreamDataAsync(streamData =>
                {
                    var index = streamData.Listeners.FindIndex(x => x.Parameter == _stream._parameterIndex);
                    if (index >= 0) streamData.Listeners.RemoveAt(index);
                });
            }
        }
    }
}