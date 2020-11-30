using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Cache;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValueBinder : IValueBinder
    {
        private readonly JObject _trigger;
        private readonly IIgniteClient _ignite;
        private readonly ILogger _logger;

        public Type Type { get; } = typeof(object).MakeByRefType();

        public PerperTriggerValueBinder(JObject trigger, IIgniteClient ignite, ILogger logger)
        {
            _trigger = trigger;
            _ignite = ignite;
            _logger = logger;
        }

        public Task<object?> GetValueAsync()
        {
            return Task.FromResult<object?>(null);
        }

        public async Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            if (_trigger.ContainsKey("Call"))
            {
                var call = (string)_trigger["Call"]!;
                var callsCache = _ignite.GetCache<string, CallData>("calls");
                var callData = await callsCache.GetAsync(call);
                callData.Result = value;
                callData.Finished = true;
                await callsCache.ReplaceAsync(call, callData);
            }
            else
            {
                var stream = (string)_trigger["Stream"]!;

                var asyncEnumerableInterface = GetGenericInterface(value.GetType(), typeof(IAsyncEnumerable<>));
                if (asyncEnumerableInterface == null)
                {
                    throw new NotSupportedException($"Expected IAsyncEnumerable<*> return from stream function, got: {value.GetType()}.");
                }

                var cacheType = asyncEnumerableInterface.GetGenericArguments()[0];
                var processMethod = GetType().GetMethod(nameof(ProcessAsyncEnumerable), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(cacheType);

                await (Task)processMethod.Invoke(this, new object[] { stream, value, cancellationToken })!;
            }
        }

        private async Task ProcessAsyncEnumerable<T>(string stream, IAsyncEnumerable<T> values, CancellationToken cancellationToken)
        {
            var collector = new PerperCollector<T>(_ignite, stream);
            await foreach (var value in values)
            {
                await collector.AddAsync(value);
            }
        }

        private Type? GetGenericInterface(Type type, Type genericInterface)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericInterface)
            {
                return type;
            }
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericInterface)
                {
                    return iface;
                }
            }
            return null;
        }

        public string ToInvokeString()
        {
            return _trigger.ToString()!;
        }
    }
}