using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IAsyncDisposable
    {
        public StreamBinaryTypeName TypeName { get; }

        private readonly IIgniteClient _igniteClient;

        public PerperFabricStream(StreamBinaryTypeName typeName, IIgniteClient igniteClient)
        {
            TypeName = typeName;

            _igniteClient = igniteClient;
        }

        public async ValueTask DisposeAsync()
        {
            var streamsCacheClient = _igniteClient.GetBinaryCache<string>("streams");
            await streamsCacheClient.RemoveAsync(TypeName.StreamName);
        }
    }
}