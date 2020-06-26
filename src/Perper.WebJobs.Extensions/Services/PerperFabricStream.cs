using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.Protocol.Cache;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IPerperStream
    {
        public StreamRef StreamRef { get; }

        public string DeclaredDelegate { get; }

        private readonly IIgniteClient _igniteClient;

        public PerperFabricStream(string streamName, IIgniteClient igniteClient, string declaredDelegate = "", bool passthrough = false)
        {
            StreamRef = new StreamRef {StreamName = streamName, Passthrough = passthrough};
            DeclaredDelegate = declaredDelegate;

            _igniteClient = igniteClient;
        }

        public IPerperStream GetRef()
        {
            return new PerperFabricStream(StreamRef.StreamName, _igniteClient, "", true);
        }

        public async ValueTask DisposeAsync()
        {
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.RemoveAsync(StreamRef.StreamName);
        }
    }
}