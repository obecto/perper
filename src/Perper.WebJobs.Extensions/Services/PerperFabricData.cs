using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Microsoft.Extensions.Logging;
using Perper.Protocol.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperFabricData
    {
        private readonly string _streamName;
        private readonly IIgniteClient _igniteClient;
        private readonly ILogger _logger;

        public PerperFabricData(string streamName, IIgniteClient igniteClient, ILogger logger)
        {
            _logger = logger;
            _streamName = streamName;
            _igniteClient = igniteClient;
        }

        public IAsyncDisposable GetStream()
        {
            var streamObject = new StreamData
            {
                Name = _streamName
            };
            return new PerperFabricStream(streamObject, _igniteClient);
        }
        
        public IAsyncDisposable DeclareStream(string name)
        {
            var streamObject = new StreamData
            {
                Name = GenerateName(name),
                Delegate = name,
            };
            return new PerperFabricStream(streamObject, _igniteClient);
        }

        public async Task<IAsyncDisposable> StreamFunctionAsync(string name, object parameters)
        {
            var streamObject = new StreamData
            {
                Name = GenerateName(name),
                Delegate = name,
                DelegateType = StreamDelegateType.Function,
                Params = CreateDelegateParameters(parameters)
            };
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamObject.Name, streamObject);
            return new PerperFabricStream(streamObject, _igniteClient);
        }

        public async Task<IAsyncDisposable> StreamFunctionAsync(PerperStreamName perperStreamName, object parameters)
        {
            var streamObject = new StreamData
            {
                Name = perperStreamName.Name,
                Delegate = perperStreamName.DelegateName,
                DelegateType = StreamDelegateType.Function,
                Params = CreateDelegateParameters(parameters)
            };
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.PutIfAbsentAsync(streamObject.Name, streamObject);
            return new PerperFabricStream(streamObject, _igniteClient);
        }

        public async Task<IAsyncDisposable> StreamFunctionAsync(IAsyncDisposable declaration, object parameters)
        {
            var streamObject = ((PerperFabricStream) declaration).StreamData;
            streamObject.DelegateType = StreamDelegateType.Function;
            streamObject.Params = CreateDelegateParameters(parameters);
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamObject.Name, streamObject);
            return declaration;
        }

        public async Task<IAsyncDisposable> StreamActionAsync(string name, object parameters)
        {
            var streamObject = new StreamData
            {
                Name = GenerateName(name),
                Delegate = name,
                DelegateType = StreamDelegateType.Action,
                Params = CreateDelegateParameters(parameters)
            };
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamObject.Name, streamObject);
            return new PerperFabricStream(streamObject, _igniteClient);
        }

        public async Task<IAsyncDisposable> StreamActionAsync(IAsyncDisposable declaration, object parameters)
        {
            var streamObject = ((PerperFabricStream) declaration).StreamData;
            streamObject.DelegateType = StreamDelegateType.Action;
            streamObject.Params = CreateDelegateParameters(parameters);
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            await streamsCache.PutAsync(streamObject.Name, streamObject);
            return declaration;
        }

        public async Task BindStreamOutputAsync(IEnumerable<IAsyncDisposable> streams)
        {
            var streamsObjects = streams.Select(s =>
                ((PerperFabricStream) s).StreamData.GetRef()).ToArray();

            if (streamsObjects.Any())
            {
                var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
                var streamObject = await streamsCacheClient.GetAsync(_streamName);
                streamObject.Params = streamObject.Params.ToBuilder().SetField("$return", streamsObjects).Build();
                await streamsCacheClient.ReplaceAsync(_streamName, streamObject);
            }
        }

        public async Task<T> FetchStreamParameterAsync<T>(string name)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            var field = streamObject.Params.GetField<object>(name);

            if (!streamObject.Params.HasField(name) && name != "context")
            {
                _logger.LogWarning($"No value found for parameter '{name}' of stream '{_streamName}'");
            }
            else if (field == null && name != "context")
            {
                _logger.LogWarning($"Null or mismatching type passed for parameter '{name}' of stream '{_streamName}'");
            }

            if (field is IBinaryObject binaryObject)
            {
                return binaryObject.Deserialize<T>();
            }

            return (T)field;
        }

        public async Task UpdateStreamParameterAsync<T>(string name, T value)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            streamObject.Params = streamObject.Params.ToBuilder().SetField(name, value).Build();
            await streamsCacheClient.ReplaceAsync(_streamName, streamObject);
        }

        public async Task AddStreamItemAsync<T>(T value)
        {
            var streamCacheClient = _igniteClient.GetOrCreateCache<long, T>(_streamName);
            await streamCacheClient.PutAsync(DateTime.UtcNow.ToFileTimeUtc(), value);
        }

        public async Task<string> CallWorkerAsync(string name, object parameters)
        {
            var workerObject = new WorkerData
            {
                Name = GenerateName(name), 
                Delegate = name,
                Params = CreateDelegateParameters(parameters)
            };
            var workersCache = _igniteClient.GetOrCreateCache<string, WorkerData>($"{_streamName}_workers");
            await workersCache.PutAsync(workerObject.Name, workerObject);
            return workerObject.Name;
        }

        public async Task<T> FetchWorkerParameterAsync<T>(string workerName, string name)
        {
            var workersCache = _igniteClient.GetCache<string, WorkerData>($"{_streamName}_workers");
            var workerObject = await workersCache.GetAsync(workerName);
            var field = workerObject.Params.GetField<object>(name);

            if (field is IBinaryObject binaryObject)
            {
                return binaryObject.Deserialize<T>();
            }

            return (T)field;
        }

        public async Task SubmitWorkerResultAsync<T>(string workerName, T value)
        {
            var workersCache = _igniteClient.GetCache<string, WorkerData>($"{_streamName}_workers");
            var workerObject = await workersCache.GetAsync(workerName);
            workerObject.Params = workerObject.Params.ToBuilder().SetField("$return", value).Build();
            await workersCache.ReplaceAsync(workerName, workerObject);
        }

        public async Task<T> ReceiveWorkerResultAsync<T>(string workerName)
        {
            var workersCache = _igniteClient.GetCache<string, WorkerData>($"{_streamName}_workers");
            var workerObject = await workersCache.GetAndRemoveAsync(workerName);
            var field = workerObject.Value.Params.GetField<object>("$return");
            if (field is IBinaryObject binaryObject)
            {
                return binaryObject.Deserialize<T>();
            }

            return (T)field;
        }

        private static string GenerateName(string delegateName)
        {
            return $"{delegateName.Replace("'", "").Replace(",", "")}-{Guid.NewGuid().ToString()}";
        }

        private IBinaryObject CreateDelegateParameters(object parameters)
        {
            var builder = _igniteClient.GetBinary().GetBuilder($"stream{Guid.NewGuid():N}");

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(parameters);
                switch (propertyValue)
                {
                    case PerperFabricStream stream:
                        builder.SetField(propertyInfo.Name, new[] {stream.StreamData.GetRef()});
                        break;
                    case IAsyncDisposable[] streams when streams.All(s => s is PerperFabricStream):
                        builder.SetField(propertyInfo.Name, streams.Select(s =>
                            ((PerperFabricStream) s).StreamData.GetRef()).ToArray());
                        break;
                    default:
                        builder.SetField(propertyInfo.Name, propertyValue);
                        break;
                }
            }

            return builder.Build();
        }
    }
}