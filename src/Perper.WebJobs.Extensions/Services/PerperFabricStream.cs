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

        public bool IsRef { get; }

        private readonly IIgniteClient _igniteClient;

        public PerperFabricStream(StreamData streamData, IIgniteClient igniteClient, bool isRef = false)
        {
            StreamData = streamData;
            IsRef = isRef;

            _igniteClient = igniteClient;
        }

        public PerperFabricStream GetRef()
        {
            return new PerperFabricStream(StreamData, _igniteClient, true);
        }

        public async ValueTask DisposeAsync()
        {
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.RemoveAsync(StreamData.Name);
        }
    }
}