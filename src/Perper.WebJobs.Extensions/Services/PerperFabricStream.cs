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

        public bool Passthrough { get; }

        private readonly IIgniteClient _igniteClient;

        public PerperFabricStream(StreamData streamData, IIgniteClient igniteClient, bool passthrough = false)
        {
            StreamData = streamData;
            Passthrough = passthrough;

            _igniteClient = igniteClient;
        }

        public IPerperStream GetRef()
        {
            return new PerperFabricStream(StreamData, _igniteClient, true);
        }

        public StreamRef GetStreamRef()
        {
            Console.WriteLine("Passthrough for {0}: {1}", StreamData.Name, Passthrough);
            return new StreamRef
            {
                StreamName = StreamData.Name,
                Passthrough = Passthrough
            };
        }

        public async ValueTask DisposeAsync()
        {
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.RemoveAsync(StreamData.Name);
        }
    }
}