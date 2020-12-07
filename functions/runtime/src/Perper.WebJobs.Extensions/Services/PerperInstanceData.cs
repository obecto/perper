using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperInstanceData
    {
        private readonly IIgniteClient _ignite;
        private readonly PerperBinarySerializer _serializer;

        private int _nextStreamParameterIndex = 0;
        private int _nextAnonymousStreamParameterIndex = 0;
        private bool _initialized = false;

        public string InstanceName { get; private set; } = default!;
        public IInstanceData InstanceData { get; private set; } = default!;

        public PerperInstanceData(IIgniteClient ignite, PerperBinarySerializer serializer)
        {
            _ignite = ignite;
            _serializer = serializer;
        }

        public async Task SetTriggerValue(JObject trigger)
        {
            if (trigger.ContainsKey("Call"))
            {
                InstanceName = (string)trigger["Call"]!;
                var callsCache = _ignite.GetCache<string, CallData>("calls");
                InstanceData = await callsCache.GetAsync(InstanceName);
            }
            else
            {
                InstanceName = (string)trigger["Stream"]!;
                var streamsCache = _ignite.GetCache<string, StreamData>("streams");
                InstanceData = await streamsCache.GetAsync(InstanceName);
            }
            _initialized = true;
        }

        public object? GetParameters(Type type)
        {
            return _serializer.GetObjectConverters(type).from.Invoke(InstanceData.Parameters);
        }

        public object?[] GetParameters<T>()
        {
            var parameters = GetParameters(typeof(object?[]));
            return (parameters as object?[]) ?? new object?[] { parameters };
        }

        public int GetStreamParameterIndex()
        {
            if (_initialized)
            {
                // Give anonymous/negative parameter indices after initialization
                return Interlocked.Decrement(ref _nextAnonymousStreamParameterIndex);
            }
            else
            {
                return Interlocked.Increment(ref _nextStreamParameterIndex);
            }
        }
    }
}