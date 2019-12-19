using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Perper.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricData
    {
        private readonly string _streamName;
        private readonly IIgniteClient _igniteClient;

        public PerperFabricData(string streamName, IIgniteClient igniteClient)
        {
            _streamName = streamName;
            _igniteClient = igniteClient;
        }

        public async Task<IAsyncDisposable> StreamFunctionAsync(string name, object parameters)
        {
            var typeName = new StreamBinaryTypeName(name, DelegateType.Function);
            var streamsCacheClient = _igniteClient.GetCache<string, IBinaryObject>("streams");
            await streamsCacheClient.PutAsync(_streamName, CreateProtocolObject(typeName, parameters));
            return new PerperFabricStream(typeName, _igniteClient);
        }

        public async Task<IAsyncDisposable> StreamActionAsync(string name, object parameters)
        {
            var typeName = new StreamBinaryTypeName(name, DelegateType.Action);
            var streamsCacheClient = _igniteClient.GetCache<string, IBinaryObject>("streams");
            await streamsCacheClient.PutAsync(_streamName, CreateProtocolObject(typeName, parameters));
            return new PerperFabricStream(typeName, _igniteClient);
        }

        public async Task<T> FetchStreamParameter<T>(string name)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, IBinaryObject>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            return streamObject.GetField<T>(name);
        }

        public async Task UpdateStreamParameterAsync<T>(string name, T value)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, IBinaryObject>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            var updatedStreamObject = streamObject.ToBuilder().SetField(name, value).Build();
            await streamsCacheClient.ReplaceAsync(_streamName, updatedStreamObject);
        }

        public async Task<T> FetchStreamParameterItemAsync<T>(string itemStreamName, long itemKey)
        {
            var streamCacheClient = _igniteClient.GetCache<long, T>(itemStreamName);
            return await streamCacheClient.GetAsync(itemKey);
        }

        public async Task AddStreamItemAsync<T>(T value)
        {
            var streamCacheClient = _igniteClient.GetCache<long, T>(_streamName);
            await streamCacheClient.PutAsync(DateTime.UtcNow.ToFileTimeUtc(), value);
        }

        public async Task InvokeWorkerAsync(object parameters)
        {
            var workersCache = _igniteClient.GetCache<string, IBinaryObject>("workers");
            await workersCache.PutAsync(_streamName, CreateProtocolObject(new WorkerBinaryTypeName(), parameters));
        }

        public async Task<T> FetchWorkerParameterAsync<T>(string name)
        {
            var workersCache = _igniteClient.GetCache<string, IBinaryObject>("workers");
            var workerObject = await workersCache.GetAsync(_streamName);
            return workerObject.GetField<T>(name);
        }

        public async Task SubmitWorkerResultAsync<T>(T value)
        {
            var workersCache = _igniteClient.GetCache<string, IBinaryObject>("workers");
            var workerObject = await workersCache.GetAsync(_streamName);
            var updatedWorkerObject = workerObject.ToBuilder().SetField("$return", value).Build();
            await workersCache.ReplaceAsync(_streamName, updatedWorkerObject);
        }
        
        public async Task<T> ReceiveWorkerResultAsync<T>()
        {
            var workersCache = _igniteClient.GetCache<string, IBinaryObject>("workers");
            var workerObject = await workersCache.GetAndRemoveAsync(_streamName);
            return workerObject.Value.GetField<T>("$return");
        }

        private IBinaryObject CreateProtocolObject(object header, object parameters)
        {
            var binary = _igniteClient.GetBinary();
            var builder = _igniteClient.GetBinary().GetBuilder(header.ToString());

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(properties);
                if (propertyValue is PerperFabricStream stream)
                {
                    builder.SetField(propertyInfo.Name, binary.GetBuilder(stream.TypeName.ToString()).Build());
                }
                else
                {
                    builder.SetField(propertyInfo.Name, propertyValue);
                }
            }

            return builder.Build();
        }
    }
}