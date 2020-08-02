using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Configuration;
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

        public IEnumerable<(string, IEnumerable<Stream>)> GetInputStreams()
        {
            var streamsCache = _ignite.GetCache<string, StreamData>("streams");

            var streamParams = StreamData.StreamParams;
            foreach (var (field, streamNames) in streamParams)
            {
                var streams = streamNames.Select(name => new Stream(streamsCache[name], _ignite));

                yield return (field, streams);
            }
        }

        public async Task UpdateAsync(CancellationToken cancellationToken)
        {
            await using var deployment = new StreamServiceDeployment(StreamData.Name, _ignite);
            var streamService = deployment.GetService();
            if (streamService != null)
            {
                await streamService.UpdateStreamAsync(StreamData);
            }
            else if (StreamData.DelegateType == StreamDelegateType.Action)
            {
                CreateCache();
                
                await deployment.DeployAsync();
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
        }

        public async IAsyncEnumerable<IEnumerable<(long, object)>> ListenAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var cache = CreateCache();

            await using var deployment = new StreamServiceDeployment(StreamData.Name, _ignite);
            await deployment.DeployAsync();
            
            await foreach (var items in cache.QueryContinuousAsync(cancellationToken))
            {
                yield return items;
            }
        }

        private ICache<long, object> CreateCache()
        {
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

            return cache;
        }
    }
}