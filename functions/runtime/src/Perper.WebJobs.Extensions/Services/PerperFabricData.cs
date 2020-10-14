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

        public IPerperStream DeclareStream(string streamName, string delegateName, Type? indexType = null)
        {
            return new PerperFabricStream(streamName, false, null, delegateName, indexType, () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
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
            return new PerperFabricStream(streamObject.Name, false, null, "", null, () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
        }

        public async Task<IPerperStream> StreamFunctionAsync(IPerperStream declaration, object parameters)
        {
            var streamObject = ((PerperFabricStream)declaration);
            await StreamFunctionAsync(streamObject.StreamName, streamObject.DeclaredDelegate, parameters, streamObject.DeclaredType);
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
            return new PerperFabricStream(streamObject.Name, false, null, "", null, () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
        }

        public async Task<IPerperStream> StreamActionAsync(IPerperStream declaration, object parameters)
        {
            var streamObject = ((PerperFabricStream)declaration);
            await StreamActionAsync(streamObject.StreamName, streamObject.DeclaredDelegate, parameters);
            return declaration;
        }

        public async Task BindStreamOutputAsync(IEnumerable<IPerperStream> streams)
        {
            var streamsNames = streams.Cast<PerperFabricStream>().Select(s => s.AsStreamParam()).ToArray();

            if (streamsNames.Any())
            {
                var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
                var streamObject = await streamsCacheClient.GetAsync(_streamName);
                streamObject.StreamParams["$return"] = streamsNames;
                await streamsCacheClient.ReplaceAsync(_streamName, streamObject);
            }
        }


        public async Task<string> FetchStreamParameterStreamNameAsync(string name)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            return streamObject.StreamParams[name].Single().Stream;
        }

        public async Task<T> FetchStreamParameterAsync<T>(string name)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);

            if (streamObject.Params.TryGetValue(name, out var value))
            {
                if (!typeof(T).IsAssignableFrom(value?.GetType()))
                {
                    _logger.LogWarning($"Null or mismatching type passed for parameter '{name}' of stream '{_streamName}'");
                }
                return (T)value!;
            }
            else
            {
                if (name != "context")
                {
                    _logger.LogWarning($"No value found for parameter '{name}' of stream '{_streamName}'");
                }
                return default(T)!;
            }
        }

        public async Task UpdateStreamParameterAsync<T>(string name, T value)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            streamObject.Params[name] = value;
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
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);

            var workerObject = new WorkerData(workerName, delegateName, caller, CreateDelegateParameters(parameters).Item1);
            streamObject.Workers[workerObject.Name] = workerObject;

            await streamsCacheClient.ReplaceAsync(_streamName, streamObject);
            return workerObject.Name;
        }

        public async Task<T> FetchWorkerParameterAsync<T>(string workerName, string name)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            var workerObject = streamObject.Workers[workerName];

            if (workerObject.Params.TryGetValue(name, out var value))
            {
                if (!typeof(T).IsAssignableFrom(value?.GetType()))
                {
                    _logger.LogWarning($"Null or mismatching type passed for parameter '{name}' of worker '{_streamName}'");
                }
                return (T)value!;
            }
            else
            {
                return default(T)!;
            }
        }

        public async Task SubmitWorkerResultAsync<T>(string workerName, T value)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            var workerObject = streamObject.Workers[workerName];
            workerObject.Params.Add("$return", value);
            workerObject.Finished = true;
            await streamsCacheClient.ReplaceAsync(_streamName, streamObject);
        }

        public async Task<T> ReceiveWorkerResultAsync<T>(string workerName)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            streamObject.Workers.Remove(workerName, out var workerObject);
            await streamsCacheClient.ReplaceAsync(_streamName, streamObject);

            return (T)workerObject!.Params["$return"]!;
        }

        private (Dictionary<string, object?>, Dictionary<string, StreamParam[]>) CreateDelegateParameters(object parameters)
        {
            var binary = _igniteClient.GetBinary();
            var dataParameters = new Dictionary<string, object?>();
            var streamParameters = new Dictionary<string, StreamParam[]>();

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(parameters);

                dataParameters.Add(propertyInfo.Name, propertyValue);

                switch (propertyValue)
                {
                    case PerperFabricStream stream:
                        if (stream.Subscribed)
                        {
                            streamParameters.Add(propertyInfo.Name, new[] { stream.AsStreamParam() });
                        }
                        break;
                    case IEnumerable<IPerperStream> streams:
                        var filteredStreams = (
                            from s in streams
                            let stream = s as PerperFabricStream
                            where stream != null && stream.Subscribed
                            select stream.AsStreamParam()).ToArray();
                        if (filteredStreams.Length > 0)
                        {
                            streamParameters.Add(propertyInfo.Name, filteredStreams);
                        }
                        break;
                }
            }

            return (dataParameters, streamParameters);
        }

        private Dictionary<string, string>? GetIndexFields(Type? indexType)
        {
            if (indexType == null)
            {
                return null;
            }

            Dictionary<string, string> indexFields = new Dictionary<string, string>();
            foreach (var item in indexType.GetProperties())
            {
                string javaType = JavaTypeMappingHelper.GetJavaTypeAsString(item.PropertyType);
                if (!String.IsNullOrEmpty(javaType))
                {
                    indexFields[item.Name] = javaType;
                }
            }

            return indexFields;
        }

        private IBinaryObject CreateEmptyFilter()
        {
            return _igniteClient.GetBinary().GetBuilder($"filter{Guid.NewGuid():N}").Build();
        }
    }
}