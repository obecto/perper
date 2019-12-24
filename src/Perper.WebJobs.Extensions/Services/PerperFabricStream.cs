using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricStream : IAsyncDisposable
    {
        private readonly IIgniteClient _igniteClient;
        public StreamBinaryTypeName TypeName { get; }
        
        public PerperFabricStream(StreamBinaryTypeName typeName, IIgniteClient igniteClient)
        {
            _igniteClient = igniteClient;
            TypeName = typeName;
        }
        
        public async ValueTask DisposeAsync()
        {
            var streamsCacheClient = _igniteClient.GetBinaryCache<string>("streams");
            await streamsCacheClient.RemoveAsync(TypeName.DelegateName);
        }
    }
}