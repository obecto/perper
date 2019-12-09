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
using Perper.Protocol.Header;

namespace Perper.Fabric.Streams
{
    //TODO: Change StreamObject to be Lazy type (Stream instances can be instantiated many times / simultaneously)
    public class Stream
    {
        public StreamHeader StreamHeader { get; }
        public IBinaryObject StreamObject { get; }

        private readonly IIgnite _ignite;

        public Stream(StreamHeader streamHeader, IBinaryObject streamObject, IIgnite ignite)
        {
            StreamHeader = streamHeader;
            StreamObject = streamObject;
            
            _ignite = ignite;
        }

        public Stream CreateChildStream(IBinaryObject childStreamObject)
        {
            var childStreamHeader = new StreamHeader(childStreamObject.GetBinaryType().TypeName);
            
            var streamsObjects = _ignite.GetCache<string, IBinaryObject>("streamsObjects");
            streamsObjects[childStreamHeader.Name] = childStreamObject;
            
            return new Stream(childStreamHeader, childStreamObject, _ignite);
        }

        public IEnumerable<Tuple<string, Stream>> GetInputStreams()
        {
            var newStream = new Func<string, Stream>(field =>
            {
                var header = new StreamHeader(StreamObject.GetField<IBinaryObject>(field).GetBinaryType().TypeName);
                return new Stream(header,
                    _ignite.GetCache<string, IBinaryObject>("streamsObjects")[header.Name], _ignite);
            });

            return
                from field in StreamObject.GetBinaryType().Fields
                where StreamObject.GetBinaryType().GetFieldTypeName(field).StartsWith(nameof(StreamHeader))
                select Tuple.Create(field, newStream(field));
        }

        public async IAsyncEnumerable<IEnumerable<IBinaryObject>> Listen(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var service = _ignite.GetServices().GetService<StreamService>(StreamHeader.Name);
            if (service == null)
            {
                service = new StreamService(this, _ignite);
                _ignite.GetServices().DeployNodeSingleton(StreamHeader.Name, service);
            }

            var cache = _ignite.GetOrCreateCache<long, IBinaryObject>(StreamHeader.Name);
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