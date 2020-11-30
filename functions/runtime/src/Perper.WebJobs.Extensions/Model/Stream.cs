using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Binary;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Cache.Notifications;

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
        [PerperInject] protected PerperInstanceData? Instance { set { // Setter used so that parameter index is updated on deserialization from parameters
            if (value != null) {
                _instance = value;
                _parameterIndex = value.GetStreamParameterIndex();
            }
        } }

        [PerperInject] protected readonly IContext _context;
        [PerperInject] protected readonly IState _state;


        #pragma warning disable 8618 // _instance and _parameterIndex come from Instance
        public Stream(string streamName, PerperInstanceData instance, FabricService fabric, IIgniteClient ignite, IContext context, IState state)
            : base(streamName, fabric, ignite)
        {
            Instance = instance;
            _context = context;
            _state = state;
        }
        #pragma warning restore 8618

        private StreamAsyncEnumerable<T> GetEnumerable(Dictionary<string, object> filter, bool localToData)
        {
            return new StreamAsyncEnumerable<T>(StreamName, _parameterIndex, filter, localToData, _state, _instance, _fabric, _ignite);
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
    }

    public class StreamAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public string StreamName { get; protected set; }
        public Dictionary<string, object> Filter { get; protected set; }
        public bool LocalToData { get; protected set; }

        [NonSerialized] private PerperInstanceData _instance;
        [NonSerialized] private int _parameterIndex;
        [PerperInject] protected PerperInstanceData? Instance { set { // Setter used so that parameter index is updated on deserialization from parameters
            if (value != null) {
                _instance = value;
                _parameterIndex = value.GetStreamParameterIndex();
            }
        } }

        [PerperInject] protected readonly IState _state;
        [PerperInject] protected readonly FabricService _fabric;
        [PerperInject] protected readonly IIgniteClient _ignite;

        public StreamAsyncEnumerable(string streamName, int parameterIndex, Dictionary<string, object> filter, bool localToData, IState state, PerperInstanceData instance, FabricService fabric, IIgniteClient ignite)
        {
            StreamName = streamName;
            Filter = filter;
            LocalToData = localToData;
            _state = state;
            _instance = instance;
            _parameterIndex = parameterIndex;
            _fabric = fabric;
            _ignite = ignite;
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

                await foreach (var (key, notification) in _fabric.GetNotifications(_instance.InstanceName, _parameterIndex, cancellationToken))
                {
                    if (notification is StreamItemNotification si)
                    {
                        var cache = _ignite.GetCache<long, T>(si.Cache);
                        var value = await cache.GetAsync(si.Key);

                        await ((State)_state).LoadStateEntries();

                        yield return value;

                        await ((State)_state).StoreStateEntries();

                        await _fabric.ConsumeNotification(key);
                    }
                }
            }
            finally
            {
                if (_parameterIndex < 0)
                {
                    await RemoveListenerAsync();
                }
            }
        }

        private async Task ModifyStreamDataAsync(Action<StreamData> modification)
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams").WithKeepBinary<string, IBinaryObject>();
            while (true) {
                var currentValue = await streamsCache.GetAsync(StreamName);
                var newValue = currentValue.Deserialize<StreamData>();
                modification(newValue);
                var newValueBinary = _ignite.GetBinary().ToBinary<IBinaryObject>(newValue);
                if (await streamsCache.ReplaceAsync(StreamName, currentValue, newValueBinary))
                {
                    break;
                };
            }
        }

        private Task AddListenerAsync()
        {
            var streamListener = new StreamListener {
                AgentDelegate = _fabric.AgentDelegate,
                Stream = _instance.InstanceName,
                Parameter = _parameterIndex,
                Filter = Filter,
                LocalToData = LocalToData,
            };
            return ModifyStreamDataAsync(streamData => {
                streamData.Listeners.Add(streamListener);
            });
        }

        private Task RemoveListenerAsync()
        {
            // If listener is anonymous (_parameter is null) then remove the listener
            return ModifyStreamDataAsync(streamData => {
                var index = streamData.Listeners.FindIndex(x => x.Parameter == _parameterIndex);
                if (index >= 0) streamData.Listeners.RemoveAt(index);
            });
        }
    }
}