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
    public class Stream<T> : IStream<T>
    {
        public string StreamName { get; set; }

        [NonSerialized] private readonly string? _parameterName;
        [NonSerialized] private readonly FabricService _fabric;
        [NonSerialized] private readonly IIgniteClient _ignite;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerable<T>().GetAsyncEnumerator(cancellationToken);
        }

        public IAsyncEnumerable<T> DataLocal()
        {
            throw new NotImplementedException();
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

    public class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public string StreamName { get; set; }
        public Dictionary<string, object> Filter { get; set; }
        public bool LocalToData { get; set; }

        [NonSerialized] private readonly string _delegateName;
        [NonSerialized] private readonly string _streamName; // Listener
        [NonSerialized] private readonly string _parameterName;
        [NonSerialized] private readonly bool _anonymous;
        [NonSerialized] private readonly FabricService _fabric;
        [NonSerialized] private readonly IIgniteClient _ignite;

        private StreamListener StreamListener {
            get => new StreamListener {
                AgentDelegate = _delegateName,
                Stream = _streamName,
                Parameter = _parameterName,
                Filter = Filter,
                LocalToData = LocalToData,
            };
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

                await foreach (var (key, notification) in _fabric.GetStreamItemNotifications(_delegateName, StreamName,
                    _parameterName, cancellationToken))
                {
                    if (notification is StreamItemNotification si)
                    {
                        var cache = _ignite.GetCache<long, T>(si.Cache);
                        var value = await cache.GetAsync(si.Index);
                        yield return value;
                        await _fabric.ConsumeNotification(_delegateName, key);
                    }
                }
            }
            finally
            {
                if (_anonymous)
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
            return ModifyStreamDataAsync(streamData => {
                if (!streamData.Listeners.Contains(StreamListener))
                {
                    streamData.Listeners.Add(StreamListener);
                }
                // else throw?
            });
        }

        private Task RemoveListenerAsync()
        {
            // If listener is anonymous (_parameter is null) then remove the listener
            return ModifyStreamDataAsync(streamData => {
                streamData.Listeners.Remove(StreamListener);
            });
        }
    }
}