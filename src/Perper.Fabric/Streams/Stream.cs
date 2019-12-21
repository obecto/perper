using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Perper.Protocol.Cache;

namespace Perper.Fabric.Streams
{
    public class Stream
    {
        public StreamBinaryTypeName StreamObjectTypeName { get; }
        private readonly IIgnite _ignite;

        public Stream(StreamBinaryTypeName streamObjectTypeName, IIgnite ignite)
        {
            StreamObjectTypeName = streamObjectTypeName;

            _ignite = ignite;
        }

        public IEnumerable<Tuple<string, Stream>> GetInputStreams()
        {
            var streamObject = _ignite.GetCache<string, IBinaryObject>("streams")[StreamObjectTypeName.DelegateName];

            var newStream = new Func<string, Stream>(field =>
            {
                var typeName =
                    StreamBinaryTypeName.Parse(streamObject.GetField<IBinaryObject>(field).GetBinaryType().TypeName);
                return new Stream(typeName, _ignite);
            });

            return
                from field in streamObject.GetBinaryType().Fields
                where streamObject.GetBinaryType().GetFieldTypeName(field).StartsWith(nameof(StreamObjectTypeName))
                select Tuple.Create(field, newStream(field));
        }

        public async IAsyncEnumerable<IEnumerable<long>> Listen(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Activate(cancellationToken);

            var cache = _ignite.GetOrCreateCache<long, IBinaryObject>(StreamObjectTypeName.DelegateName);
            await foreach (var items in cache.GetKeysAsync(cancellationToken))
            {
                yield return items;
            }
        }

        public Task Activate(CancellationToken cancellationToken)
        {
            var service = _ignite.GetServices().GetService<StreamService>(StreamObjectTypeName.DelegateName);
            if (service == null)
            {
                service = new StreamService(this, _ignite);
                _ignite.GetServices().DeployNodeSingleton(StreamObjectTypeName.DelegateName, service);
            }
            return Task.CompletedTask;
        } 
    }
}