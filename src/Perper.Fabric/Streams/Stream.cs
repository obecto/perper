using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Event;
using Apache.Ignite.Core.Cache.Query.Continuous;
using Ignite.Extensions;

namespace Perper.Fabric.Streams
{
    //TODO: Change CacheObject to be Lazy type (Stream instances can be instantiated many times / simultaneously)
    public class Stream
    {
        public string CacheName { get; }
        public string CacheType { get; }
        public IBinaryObject CacheObject { get; }

        private readonly IIgnite _ignite;

        public Stream(string cacheName, string cacheType, IBinaryObject cacheObject, IIgnite ignite)
        {
            CacheName = cacheName;
            CacheType = cacheType;
            CacheObject = cacheObject;
            
            _ignite = ignite;
        }

        public Stream CreateChildStream(IBinaryObject childCacheObject)
        {
            var (childCacheName, childCacheType) = childCacheObject.ParseCacheObjectTypeName();
            
            var cacheObjects = _ignite.GetCache<string, IBinaryObject>("cacheObjects");
            cacheObjects[childCacheName] = childCacheObject;
            
            return new Stream(childCacheName, childCacheType, childCacheObject, _ignite);
        }

        public IEnumerable<Tuple<string, Stream>> GetInputStreams()
        {
            var cacheObjects = _ignite.GetCache<string, IBinaryObject>("cacheObjects");
            foreach (var (refCacheParam, refCacheName, refCacheType) in CacheObject.SelectReferencedCacheObjects())
            {
                yield return Tuple.Create(refCacheParam,
                    new Stream(refCacheName, refCacheType, cacheObjects[refCacheName], _ignite));
            }
        }

        public async IAsyncEnumerable<IEnumerable<IBinaryObject>> Listen(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var service = _ignite.GetServices().GetService<StreamService>(CacheName);
            if (service == null)
            {
                service = new StreamService(this, _ignite);
                _ignite.GetServices().DeployNodeSingleton(CacheName, service);
            }

            var cache = _ignite.GetOrCreateCache<long, IBinaryObject>(CacheName);
            while (!cancellationToken.IsCancellationRequested)
            {
                var queryTask = new TaskCompletionSource<IEnumerable<IBinaryObject>>();
                var listener = new LocalListener(events => queryTask.SetResult(events.Select(e => e.Value)));

                //TODO: Optimize the use of query handles
                using (cache.QueryContinuous(new ContinuousQuery<long, IBinaryObject>(listener)))
                {
                    yield return await queryTask.Task;
                }
            }
        }

        private class LocalListener : ICacheEntryEventListener<long, IBinaryObject>
        {
            private readonly Action<IEnumerable<ICacheEntryEvent<long, IBinaryObject>>> _callback;

            public LocalListener(Action<IEnumerable<ICacheEntryEvent<long, IBinaryObject>>> callback)
            {
                _callback = callback;
            }

            public void OnEvent(IEnumerable<ICacheEntryEvent<long, IBinaryObject>> events)
            {
                _callback(events);
            }
        }
    }
}