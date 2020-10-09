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
            return new PerperFabricStream(_streamName, CreateEmptyFilter(), false, false);
        }

        public IPerperStream DeclareStream(string streamName, string delegateName, Type? indexType = null)
        {
            return new PerperFabricStream(streamName, CreateEmptyFilter(), false, false, delegateName, indexType, () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
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
            return new PerperFabricStream(streamObject.Name, CreateEmptyFilter(), false, false, "", null, () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
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
            return new PerperFabricStream(streamObject.Name, CreateEmptyFilter(), false, false, "", null, () => _igniteClient.GetCache<string, StreamData>("streams").RemoveAsync(streamName));
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
                return DeserializeBinaryObject<T>(binaryObject);
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
            var field = workerObject.Params.GetField<object>(name);

            if (field is IBinaryObject binaryObject)
            {
                return DeserializeBinaryObject<T>(binaryObject);
            }

            return (T)field;
        }

        public async Task SubmitWorkerResultAsync<T>(string workerName, T value)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            var workerObject = streamObject.Workers[workerName];
            workerObject.Params = workerObject.Params.ToBuilder().SetField("$return", value).Build();
            await streamsCacheClient.ReplaceAsync(_streamName, streamObject);
        }

        public async Task<T> ReceiveWorkerResultAsync<T>(string workerName)
        {
            var streamsCacheClient = _igniteClient.GetCache<string, StreamData>("streams");
            var streamObject = await streamsCacheClient.GetAsync(_streamName);
            streamObject.Workers.Remove(workerName, out var workerObject);
            await streamsCacheClient.ReplaceAsync(_streamName, streamObject);

            var field = workerObject!.Params.GetField<object>("$return");
            if (field is IBinaryObject binaryObject)
            {
                return DeserializeBinaryObject<T>(binaryObject);
            }

            return (T)field;
        }

        private (IBinaryObject, Dictionary<string, StreamParam[]>) CreateDelegateParameters(object parameters)
        {
            var binary = _igniteClient.GetBinary();
            var builder = binary.GetBuilder($"stream{Guid.NewGuid():N}");
            var streamParameters = new Dictionary<string, StreamParam[]>();

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(parameters);

                // HACK: Needed to serialize PerperFabricStream properly; Passing propertyValue directly to SetField
                // causes WriteObject<IBinaryObject> called by PerperFabricStream.WriteBinary to misbehave,
                // writing the object without the BinaryTypeId.Binary tag in front.
                var propertyValueSerialized = binary.ToBinary<object>(propertyValue);

                builder.SetField(propertyInfo.Name, propertyValueSerialized);
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

            return (builder.Build(), streamParameters);
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

        public T DeserializeBinaryObject<T>(IBinaryObject binary)
        {
            // HACK: Workaround for IGNITE-13563 in Ignite 2.8.1
            var methods = binary.GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var method in methods)
            {
                if (method.Name == "Deserialize" && method.IsPrivate)
                {
                    var methodT = method.MakeGenericMethod(typeof(T));
                    // var paramType = methodT.GetParameters()[0].ParameterType;
                    return (T) methodT.Invoke(binary, new object[] {1})!;
                }
            }
            return binary.Deserialize<T>();
        }
    }
}