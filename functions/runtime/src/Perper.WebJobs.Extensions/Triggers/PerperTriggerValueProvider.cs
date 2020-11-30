using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Cache;
using Perper.WebJobs.Extensions.Model;
using ITuple = System.Runtime.CompilerServices.ITuple;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValueProvider : IValueProvider
    {
        private readonly JObject _trigger;
        private readonly IIgniteClient _ignite;
        private readonly ILogger _logger;
        private readonly IServiceProvider _services;

        public Type Type { get; }

        public PerperTriggerValueProvider(JObject trigger, Type type, IServiceProvider services, IIgniteClient ignite, ILogger logger)
        {
            _trigger = trigger;
            Type = type;
            _services = services;
            _ignite = ignite;
            _logger = logger;
        }

        public async Task<object?> GetValueAsync()
        {
            object?[]? parameters;
            if (_trigger.ContainsKey("Call"))
            {
                var callsCache = _ignite.GetCache<string, CallData>("calls");
                var callData = await callsCache.GetAsync((string)_trigger["Call"]!);
                parameters = callData.Parameters;
            }
            else
            {
                var streamsCache = _ignite.GetCache<string, StreamData>("streams");
                var streamData = await streamsCache.GetAsync((string)_trigger["Stream"]!);
                parameters = streamData.Parameters;
            }

            var streamHelper = (StreamParameterIndexHelper) _services.GetService(typeof(StreamParameterIndexHelper));
            streamHelper.Anonymous = true;

            if (Type.IsAssignableFrom(typeof(object[])))
            {
                return (object?) parameters!;
            }

            if (typeof(ITuple).IsAssignableFrom(Type))
            {
                return Activator.CreateInstance(Type, parameters)!;
            }

            return parameters?[0];
        }

        public string ToInvokeString()
        {
            return _trigger.ToString()!;
        }
    }
}