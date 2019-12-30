using System;
using System.Collections.Generic;
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

        public IEnumerable<(string, Stream)> GetInputStreams()
        {
            var streamObject = _ignite.GetBinaryCache<string>("streams")[StreamObjectTypeName.StreamName];

            foreach (var field in streamObject.GetBinaryType().Fields)
            {
                string fieldType;
                try
                {
                    fieldType = streamObject.GetField<IBinaryObject>(field).GetBinaryType().TypeName;
                }
                catch (InvalidCastException)
                {
                    continue;
                }

                if (fieldType.StartsWith(nameof(StreamBinaryTypeName)))
                {
                    yield return (field, new Stream(StreamBinaryTypeName.Parse(fieldType), _ignite));
                }
            }
        }

        public async IAsyncEnumerable<IEnumerable<(long, IBinaryObject)>> ListenAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var deployment = new StreamServiceDeployment(_ignite, StreamObjectTypeName.ToString());
            await deployment.DeployAsync();
            
            var cache = _ignite.GetOrCreateBinaryCache<long>(StreamObjectTypeName.StreamName);
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