using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Triggers;

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
        public string Agent { get; private set; } = default!;
        public object? Parameters { get; private set; } = default!;

        public PerperInstanceData(IIgniteClient ignite, PerperBinarySerializer serializer)
        {
            _ignite = ignite;
            _serializer = serializer;
        }

        public async Task SetTriggerValue(PerperTriggerValue triggerValue)
        {
            InstanceName = triggerValue.InstanceName;
            var cache = _ignite.GetCache<string, IInstanceData>(triggerValue.IsCall ? "calls" : "streams");
            var instanceData = await cache.GetAsync(InstanceName);
            Parameters = instanceData.Parameters;
            Agent = instanceData.Agent;
            _initialized = true;
        }

        public object? GetParameters(Type type)
        {
            return _serializer.Deserialize(Parameters, type);
        }

        public object?[] GetParameters()
        {
            var parameters = GetParameters(typeof(object?[]));
            return (parameters as object?[]) ?? new object?[] { parameters };
        }

        public T GetParameters<T>()
        {
            return (T) GetParameters(typeof(T))!;
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