using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.Protocol.Cache;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IPerperStream
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