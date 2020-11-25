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

        public Stream(string streamName, FabricService fabric, IIgniteClient ignite) {
            StreamName = streamName;
            _fabric = fabric;
            _ignite = ignite;
        }
    }

    public class Stream<T> : Stream, IStream<T>
    {
        [NonSerialized] public string? FunctionName; // HACK: Used for Declare/InitiaizeStream
        [NonSerialized] public int? ParameterIndex;
        [PerperInject] protected readonly IContext _context;
        [PerperInject] protected readonly IServiceProvider _services;

        public Stream(string streamName, FabricService fabric, IIgniteClient ignite, IContext context, IServiceProvider services)
            : base(streamName, fabric, ignite)
        {
            _context = context;
            _services = services;
        }

        private StreamAsyncEnumerable<T> GetEnumerable(Dictionary<string, object> filter, bool localToData)
        {
            return new StreamAsyncEnumerable<T>(StreamName, ParameterIndex, filter, localToData, _context, _fabric, _ignite, _services);
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

        [NonSerialized] private readonly int? _parameterIndex;
        [PerperInject] protected readonly IContext _context;
//         [PerperInject] protected readonly State _state;
        [PerperInject] protected readonly FabricService _fabric;
        [PerperInject] protected readonly IIgniteClient _ignite;
        [PerperInject] protected readonly IServiceProvider _services;
        private Context context { get => (Context)_context; }

        public StreamAsyncEnumerable(string streamName, int? parameterIndex, Dictionary<string, object> filter, bool localToData, IContext context, FabricService fabric, IIgniteClient ignite, IServiceProvider services) {
            StreamName = streamName;
            _parameterIndex = parameterIndex;
            Filter = filter;
            LocalToData = localToData;
            _context = context;
            _fabric = fabric;
            _ignite = ignite;
            _services = services;
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var parameterIndex = _parameterIndex ?? Interlocked.Decrement(ref context.NextLocalStreamParameterIndex);
            try
            {
                await AddListenerAsync(parameterIndex);

                await foreach (var (key, notification) in _fabric.GetNotifications(context.InstanceName, parameterIndex, cancellationToken))
                {
                    if (notification is StreamItemNotification si)
                    {
                        var cache = _ignite.GetCache<long, T>(si.Cache);
                        var value = await cache.GetWithServicesAsync(si.Key, _services);
//                         await _state.LoadStateEntries();
                        yield return value;
//                         await _state.StoreStateEntries();
                        await _fabric.ConsumeNotification(key);
                    }
                }
            }
            finally
            {
                if (_parameterIndex == null)
                {
                    await RemoveListenerAsync(parameterIndex);
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

        private Task AddListenerAsync(int parameterIndex)
        {
            var streamListener = new StreamListener {
                AgentDelegate = _fabric.AgentDelegate,
                Stream = context.InstanceName,
                Parameter = parameterIndex,
                Filter = Filter,
                LocalToData = LocalToData,
            };
            return ModifyStreamDataAsync(streamData => {
                streamData.Listeners.Add(streamListener);
            });
        }

        private Task RemoveListenerAsync(int parameterIndex)
        {
            // If listener is anonymous (_parameter is null) then remove the listener
            return ModifyStreamDataAsync(streamData => {
                var index = streamData.Listeners.FindIndex(x => x.Parameter == parameterIndex);
                if (index >= 0) streamData.Listeners.RemoveAt(index);
            });
        }
    }
}