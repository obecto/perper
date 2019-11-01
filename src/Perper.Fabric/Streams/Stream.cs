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
using Apache.Ignite.Core.Client;
using Perper.Fabric.Services;

namespace Perper.Fabric.Streams
{
    public class Stream
    {
        private readonly string _name;
        private readonly IIgnite _ignite;
        private readonly StreamsCache _streamsCache;
        
        private readonly IBinaryObject _parameters;
        private StreamService _streamService;
        
        public Stream(string name, IIgnite ignite, StreamsCache streamsCache)
        {
            _name = name;
            _ignite = ignite;
            _streamsCache = streamsCache;
            
            var cache = ignite.GetCache<string, IBinaryObject>("streams");
            _parameters = cache[name];
        }

        public async IAsyncEnumerable<IEnumerable<IBinaryObject>> Listen([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var streamCache = _ignite.GetOrCreateCache<long, IBinaryObject>(_name);
            if (_streamService == null)
            {
                var inputs = new List<Stream>();
                foreach (var field in _parameters.GetBinaryType().Fields)
                {
                    if (_parameters.GetBinaryType().GetFieldTypeName(field).Contains("IPerperStream"))
                    {
                        inputs.Add(_streamsCache.GetStream(field));
                    }
                }

                _streamService = new StreamService(_name, inputs);
                _ignite.GetServices().DeployNodeSingleton(_name, _streamService);
            }

            //TODO: Optimize the use of query handles
            while (!cancellationToken.IsCancellationRequested)
            {
                var queryTask = new TaskCompletionSource<IEnumerable<IBinaryObject>>();
                var listener = new LocalListener(events => queryTask.SetResult(events.Select(e => e.Value)));
                using (streamCache.QueryContinuous(new ContinuousQuery<long, IBinaryObject>(listener)))
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