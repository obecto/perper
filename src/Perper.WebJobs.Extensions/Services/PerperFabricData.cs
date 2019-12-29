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
            var typeName = new StreamBinaryTypeName(Guid.NewGuid().ToString(), name, DelegateType.Function);
            var streamsCacheClient = _igniteClient.GetBinaryCache<string>("streams");
            await streamsCacheClient.PutAsync(typeName.StreamName, CreateProtocolObject(typeName, parameters));
            return new PerperFabricStream(typeName, _igniteClient);
        }

        public async Task<IAsyncDisposable> StreamActionAsync(string name, object parameters)
        {
            var typeName = new StreamBinaryTypeName(Guid.NewGuid().ToString(), name, DelegateType.Action);
            var streamsCacheClient = _igniteClient.GetBinaryCache<string>("streams");
            await streamsCacheClient.PutAsync(typeName.StreamName, CreateProtocolObject(typeName, parameters));
            return new PerperFabricStream(typeName, _igniteClient);
        }

        public async Task<T> FetchStreamParameterAsync<T>(string name)
        {
            var streamsCacheClient = _igniteClient.GetBinaryCache<string>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            var field = streamObject.GetField<T>(name);
            if (field is IBinaryObject binaryObject)
            {
                return binaryObject.Deserialize<T>();
            }

            return field;
        }

        public async Task UpdateStreamParameterAsync<T>(string name, T value)
        {
            var streamsCacheClient = _igniteClient.GetBinaryCache<string>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            var updatedStreamObject = streamObject.ToBuilder().SetField(name, value).Build();
            await streamsCacheClient.ReplaceAsync(_streamName, updatedStreamObject);
        }

        public async Task AddStreamItemAsync<T>(T value)
        {
            var streamCacheClient = _igniteClient.GetCache<long, T>(_streamName);
            await streamCacheClient.PutAsync(DateTime.UtcNow.ToFileTimeUtc(), value);
        }

        public async Task<T> FetchStreamParameterStreamItemAsync<T>(string itemStreamName, long itemKey)
        {
            var itemStreamCacheClient = _igniteClient.GetCache<long, T>(itemStreamName);
            return await itemStreamCacheClient.GetAsync(itemKey);
        }
        
        public async Task InvokeWorkerAsync(object parameters)
        {
            var workersCache = _igniteClient.GetBinaryCache<string>("workers");
            await workersCache.PutAsync(_streamName, CreateProtocolObject(new WorkerBinaryTypeName(), parameters));
        }

        public async Task<T> FetchWorkerParameterAsync<T>(string name)
        {
            var workersCache = _igniteClient.GetBinaryCache<string>("workers");
            var workerObject = await workersCache.GetAsync(_streamName);
            var field = workerObject.GetField<T>(name);
            if (field is IBinaryObject binaryObject)
            {
                return binaryObject.Deserialize<T>();
            }

            return field;
        }

        public async Task SubmitWorkerResultAsync<T>(T value)
        {
            var workersCache = _igniteClient.GetBinaryCache<string>("workers");
            var workerObject = await workersCache.GetAsync(_streamName);
            var updatedWorkerObject = workerObject.ToBuilder().SetField("$return", value).Build();
            await workersCache.ReplaceAsync(_streamName, updatedWorkerObject);
        }
        
        public async Task<T> ReceiveWorkerResultAsync<T>()
        {
            var workersCache = _igniteClient.GetBinaryCache<string>("workers");
            var workerObject = await workersCache.GetAndRemoveAsync(_streamName);
            var field = workerObject.Value.GetField<T>("$return");
            if (field is IBinaryObject binaryObject)
            {
                return binaryObject.Deserialize<T>();
            }

            return field;
        }

        private IBinaryObject CreateProtocolObject(object header, object parameters)
        {
            var binary = _igniteClient.GetBinary();
            var builder = _igniteClient.GetBinary().GetBuilder(header.ToString());

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(parameters);
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