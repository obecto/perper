using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Linq;
using Microsoft.Extensions.Logging;
using Perper.Protocol.Cache;
using Perper.WebJobs.Extensions.Model;

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

        public IPerperStream GetStream()
        {
            return new PerperFabricStream(_streamName);
        }

        public IPerperStream DeclareStream(string streamName, string delegateName)
        {
            return new PerperFabricStream(streamName, true, delegateName, () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
        }

        public async Task<IPerperStream> StreamFunctionAsync(string streamName, string delegateName, object parameters, Type? indexType = null)
        {
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            var streamGetResult = await streamsCache.TryGetAsync(streamName);

            var streamObject = streamGetResult.Value;
            if (!streamGetResult.Success)
            {
                var (dataParams, streamParams) = CreateDelegateParameters(parameters);
                streamObject = new StreamData(streamName, delegateName, StreamDelegateType.Function, dataParams, streamParams, indexType?.FullName, GetIndexFields(indexType));
            }

            streamObject.LastModified = DateTime.UtcNow;
            await streamsCache.PutAsync(streamObject.Name, streamObject);
            return new PerperFabricStream(streamObject.Name, false, "", () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
        }

        public async Task<IPerperStream> StreamFunctionAsync(IPerperStream declaration, object parameters)
        {
            var streamObject = ((PerperFabricStream)declaration);
            await StreamFunctionAsync(streamObject.StreamName, streamObject.DeclaredDelegate, parameters);
            return declaration;
        }

        public async Task<IPerperStream> StreamActionAsync(string streamName, string delegateName, object parameters)
        {
            var streamsCache = _igniteClient.GetCache<string, StreamData>("streams");
            var streamGetResult = await streamsCache.TryGetAsync(streamName);

            var streamObject = streamGetResult.Value;
            if (!streamGetResult.Success)
            {
                var (dataParams, streamParams) = CreateDelegateParameters(parameters);
                streamObject = new StreamData(streamName, delegateName, StreamDelegateType.Action, dataParams, streamParams);
            }

            streamObject.LastModified = DateTime.UtcNow;
            await streamsCache.PutAsync(streamObject.Name, streamObject);
            return new PerperFabricStream(streamObject.Name, false, "", () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
        }

        public async Task<IPerperStream> StreamActionAsync(IPerperStream declaration, object parameters)
        {
            var streamObject = ((PerperFabricStream)declaration);
            await StreamActionAsync(streamObject.StreamName, streamObject.DeclaredDelegate, parameters);
            return declaration;
        }

        public async Task BindStreamOutputAsync(IEnumerable<IPerperStream> streams)
        {
            var streamsNames = streams.Select(s => ((PerperFabricStream)s).StreamName).ToArray();

            if (streamsNames.Any())
            {
                var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
                var streamObject = await streamsCacheClient.GetAsync(_streamName);
                streamObject.StreamParams["$return"] = streamsNames;
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

            return (T)field!;
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
            var streamCacheClient = _igniteClient.GetCache<long, T>(_streamName);
            await streamCacheClient.PutAsync(DateTime.UtcNow.ToFileTimeUtc(), value);
        }

        public IQueryable<T> QueryStreamItemsAsync<T>()
        {
            var streamCacheClient = _igniteClient.GetCache<long, T>(_streamName).AsCacheQueryable();

            return streamCacheClient.Select(entry => entry.Value);
        }

        public async Task<string> CallWorkerAsync(string workerName, string delegateName, string caller, object parameters)
        {
            var workerObject = new WorkerData(workerName, delegateName, caller, CreateDelegateParameters(parameters).Item1);
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

        private (IBinaryObject, Dictionary<string, string[]>) CreateDelegateParameters(object parameters)
        {
            var builder = _igniteClient.GetBinary().GetBuilder($"stream{Guid.NewGuid():N}");
            var streamParameters = new Dictionary<string, string[]>();

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(parameters);
                builder.SetField(propertyInfo.Name, propertyValue);
                switch (propertyValue)
                {
                    case PerperFabricStream stream:
                        if (stream.Subscribed)
                        {
                            streamParameters.Add(propertyInfo.Name, new[] { stream.StreamName });
                        }
                        break;
                    case IEnumerable<IPerperStream> streams:
                        var filteredStreams = (
                            from s in streams
                            let stream = s as PerperFabricStream
                            where stream != null && stream.Subscribed
                            select stream.StreamName).ToArray();
                        if (filteredStreams.Length > 0)
                        {
                            streamParameters.Add(propertyInfo.Name, filteredStreams);
                        }
                        break;
                }
            }

            return (builder.Build(), streamParameters);
        }

        private IEnumerable<KeyValuePair<string, string>>? GetIndexFields(Type? indexType)
        {
            if (indexType == null)
            {
                return null;
            }

            List<KeyValuePair<string, string>> indexFields = new List<KeyValuePair<string, string>>();
            foreach (var item in indexType.GetProperties())
            {
                string javaType = JavaTypeMappingHelper.GetJavaTypeAsString(item.PropertyType);
                if (!String.IsNullOrEmpty(javaType))
                {
                    indexFields.Add(new KeyValuePair<string, string>(item.Name, javaType));
                }
            }

            return indexFields;
        }
    }
}