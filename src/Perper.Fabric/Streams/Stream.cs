using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Perper.Fabric.Services;
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
            var streamObject = _ignite.GetBinaryCache<string>("streams")[StreamObjectTypeName.DelegateName];

            var newStream = new Func<string, Stream>(field =>
            {
                var typeName =
                    StreamBinaryTypeName.Parse(streamObject.GetField<IBinaryObject>(field).GetBinaryType().TypeName);
                return new Stream(typeName, _ignite);
            });

            return
                from field in streamObject.GetBinaryType().Fields
                where streamObject.GetField<IBinaryObject>(field).GetBinaryType().TypeName
                    .StartsWith(nameof(StreamBinaryTypeName))
                select Tuple.Create(field, newStream(field));
        }

        public async IAsyncEnumerable<IEnumerable<(long, IBinaryObject)>> ListenAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var deployment = new StreamServiceDeployment(_ignite, StreamObjectTypeName.ToString());
            await deployment.DeployAsync();
            
            var cache = _ignite.GetOrCreateBinaryCache<long>(StreamObjectTypeName.DelegateName);
            await foreach (var items in cache.QueryContinuousAsync(cancellationToken))
            {
                yield return items;
            }
        }

        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            await using var deployment = new StreamServiceDeployment(_ignite, StreamObjectTypeName.ToString());
            await deployment.DeployAsync();
            
            var tcs = new TaskCompletionSource<bool>();
            await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>) s).TrySetResult(true), tcs))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}