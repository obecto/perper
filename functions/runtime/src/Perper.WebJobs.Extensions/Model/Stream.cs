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

        [PerperInject] protected readonly PerperBinarySerializer _serializer;

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
        public Stream(string streamName, PerperInstanceData instance, FabricService fabric, IIgniteClient ignite, PerperBinarySerializer serializer, IState state)
            : base(streamName, fabric, ignite)
        {
            Instance = instance;
            _serializer = serializer;
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
                var convertersInfo = _stream._serializer.GetRootObjectConverters(typeof(T));
                var converter = convertersInfo.from;
                var shouldKeepBinary = convertersInfo.convertedType == typeof(IBinaryObject);

                try
                {
                    await AddListenerAsync();

                    await foreach (var (key, notification) in _stream._fabric.GetNotifications(_stream._instance.InstanceName, _stream._parameterIndex, cancellationToken))
                    {
                        if (notification is StreamItemNotification si)
                        {
                            var cache = _stream._ignite.GetCache<long, object>(si.Cache);
                            if (shouldKeepBinary)
                            {
                                cache = cache.WithKeepBinary<long, object>();
                            }

                            var value = await cache.GetAsync(si.Key);

                            await ((State)_stream._state).LoadStateEntries();

                            yield return (T)converter.Invoke(value)!;

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