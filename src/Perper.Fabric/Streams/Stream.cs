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
            
            var streamDelegateParams = StreamData.Params;
            foreach (var field in streamDelegateParams.GetBinaryType().Fields)
            {
                IBinaryObject[] fieldValues;
                try
                {
                    fieldValues = streamDelegateParams.GetField<IBinaryObject[]>(field);
                }
                catch (TypeInitializationException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                    continue;
                }

                var streams = 
                    from value in fieldValues
                    where value.GetBinaryType().TypeName.Contains(nameof(StreamRef))
                    select new Stream(streamsCache[value.Deserialize<StreamRef>().StreamName], _ignite);

                yield return (field, streams);
            }
        }

        public async IAsyncEnumerable<IEnumerable<(long, IBinaryObject)>> ListenAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var deployment = new StreamServiceDeployment(StreamData.Name, _ignite);
            await deployment.DeployAsync();
            
            var cache = _ignite.GetOrCreateBinaryCache<long>(StreamData.Name);
            await foreach (var items in cache.QueryContinuousAsync(cancellationToken))
            {
                yield return items;
            }
        }

        public async Task ActivateAsync(CancellationToken cancellationToken)
        {
            await using var deployment = new StreamServiceDeployment(StreamData.Name, _ignite);
            await deployment.DeployAsync();
            
            var tcs = new TaskCompletionSource<bool>();
            await using (cancellationToken.Register(s => ((TaskCompletionSource<bool>) s).TrySetResult(true), tcs))
            {
                await tcs.Task.ConfigureAwait(false);
            }
        }
    }
}