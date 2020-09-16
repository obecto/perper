using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Cache.Event;
using Apache.Ignite.Core.Cache.Query.Continuous;
using Apache.Ignite.Core.Resource;
using Perper.Protocol.Cache;

namespace Perper.Fabric.Streams
{
    public class Stream
    {
        public StreamData StreamData { get; }
        private readonly IIgnite _ignite;

        public Stream(StreamData streamData, IIgnite ignite)
        {
            StreamData = streamData;

            _ignite = ignite;
        }

        public IEnumerable<(string, IEnumerable<(Stream, string?, object?)>)> GetInputStreams()
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");

            var streamParams = StreamData.StreamParams;
            foreach (var (field, streamNames) in streamParams)
            {
                var streams = streamNames.Select(name => (new Stream(streamsCache[name.Item1], _ignite), name.Item2, name.Item3));

                yield return (field, streams);
            }
        }

        public async Task UpdateAsync()
        {
            if (StreamData.DelegateType == StreamDelegateType.Action)
            {
                await _ignite.GetServices().GetService<StreamService>(nameof(StreamService)).EngageStreamAsync(this);
            }
            await _ignite.GetServices().GetService<StreamService>(nameof(StreamService)).UpdateStreamAsync(this);
        }

        public async Task EngageStreamAsync()
        {
            CreateCache();
            await _ignite.GetServices().GetService<StreamService>(nameof(StreamService)).EngageStreamAsync(this);
        }

        private void CreateCache()
        {
            if (_ignite.GetCacheNames().Contains(StreamData.Name)) return;

            ICache<long, object>? cache;
            if (!string.IsNullOrEmpty(StreamData.IndexType) && StreamData.IndexFields != null &&
                StreamData.IndexFields.Any())
            {
                QueryEntity queryEntity = new QueryEntity
                {
                    KeyType = typeof(long),
                    ValueTypeName = StreamData.IndexType,
                    Fields = StreamData.IndexFields
                        .Select(item => new QueryField {Name = item.Key, FieldTypeName = item.Value}).ToList()
                };

                CacheConfiguration cacheConfiguration = new CacheConfiguration(StreamData.Name, queryEntity);
                cache = _ignite.CreateBinaryCache<long>(cacheConfiguration);
            }
            else
            {
                cache = _ignite.CreateBinaryCache<long>(StreamData.Name);
            }

            // Consider alternative implementation
            if (_ignite.GetAtomicLong($"{StreamData.Name}_Query", 0, true).CompareExchange(1, 0) == 0)
            {
                //Dispose handle?
                cache.QueryContinuous(new ContinuousQuery<long, object>(new EmptyListener())
                {
                    Filter = new RemoteFilter(StreamData.Name)
                });
            }
        }
    }

    public class EmptyListener : ICacheEntryEventListener<long, object>
    {
        public void OnEvent(IEnumerable<ICacheEntryEvent<long, object>> evts)
        {
        }
    }

    public class RemoteFilter : ICacheEntryEventFilter<long, object>
    {
        [InstanceResource] private IIgnite _ignite;

        private readonly string _stream;

        public RemoteFilter(string stream)
        {
            _stream = stream;
        }

        public bool Evaluate(ICacheEntryEvent<long, object> evt)
        {
            _ignite.GetServices().GetService<StreamService>(nameof(StreamService))
                .UpdateStreamItemAsync(_stream, evt.Key);
            return false;
        }
    }
}