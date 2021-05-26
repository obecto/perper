using System;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Newtonsoft.Json.Linq;

using Perper.WebJobs.Extensions.Cache;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperInstanceData
    {
        private object? parameters = default!;
        private readonly IIgniteClient _ignite;
        private readonly PerperBinarySerializer _serializer;

        private int _nextStreamParameterIndex;
        private int _nextAnonymousStreamParameterIndex;
        private bool _initialized;

        public string InstanceName { get; private set; } = default!;
        public string Agent { get; private set; } = default!;

        public PerperInstanceData(IIgniteClient ignite, PerperBinarySerializer serializer)
        {
            _ignite = ignite;
            _serializer = serializer;
        }

        public async Task SetTriggerValue(JObject trigger)
        {
            // Done using binary, since this.Agent is sometimes needed while deserializing Parameters
            ICacheClient<string, IBinaryObject> instanceCache;
            if (trigger.ContainsKey("Call"))
            {
                InstanceName = (string)trigger["Call"]!;
                instanceCache = _ignite.GetCache<string, CallData>("calls").WithKeepBinary<string, IBinaryObject>();
            }
            else
            {
                InstanceName = (string)trigger["Stream"]!;
                instanceCache = _ignite.GetCache<string, StreamData>("streams").WithKeepBinary<string, IBinaryObject>();
            }

            var instanceDataBinary = await instanceCache.GetAsync(InstanceName);

            Agent = instanceDataBinary.GetField<string>(nameof(IInstanceData.Agent));
            parameters = instanceDataBinary.GetField<object>(nameof(IInstanceData.Parameters));

            _initialized = true;
        }

        public object? GetParameters(Type type) => _serializer.Deserialize(parameters, type);

        public object?[] GetParameters()
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