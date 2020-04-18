using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IAsyncDisposable
    {
        public StreamData StreamData { get; }

        private readonly IIgniteClient _igniteClient;

        public PerperFabricStream(StreamData streamData, IIgniteClient igniteClient)
        {
            StreamData = streamData;

            _igniteClient = igniteClient;
        }

        public async ValueTask DisposeAsync()
        {
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.RemoveAsync(StreamData.Name);
        }
    }
}