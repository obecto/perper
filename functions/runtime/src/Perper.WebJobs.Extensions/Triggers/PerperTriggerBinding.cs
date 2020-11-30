using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Cache;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBinding : ITriggerBinding
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        private readonly ParameterInfo _parameter;
        private readonly JObject? _parameterExpression;

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; }
        public Type TriggerValueType { get; } = typeof(JObject);

        public PerperTriggerBinding(ParameterInfo parameter, PerperTriggerAttribute attribute,
            FabricService fabric, IIgniteClient ignite, IServiceProvider services, ILogger logger)
        {
            _parameter = parameter;
            _fabric = fabric;
            _ignite = ignite;
            _services = services;
            _logger = logger;

            _parameterExpression = attribute.ParameterExpression is null
                ? null
                : JObject.Parse(attribute.ParameterExpression);

            BindingDataContract = CreateBindingDataContract();
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult<IListener>(new PerperTriggerListener(
                _fabric, context.Descriptor.ShortName, _ignite, context.Executor, _logger));
        }

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext valueBindingContext)
        {
            var trigger = (JObject) value;

            var context = (Context)_services.GetService(typeof(IContext));
            await context.SetTriggerValue(trigger);

            var valueProvider = new PerperTriggerValueProvider(trigger, _parameter.ParameterType, _services, _ignite, _logger);
            var returnValueProvider = new PerperTriggerValueBinder(trigger, _ignite, _logger);
            var bindingData = await GetBindingData(trigger);

            return new TriggerData(valueProvider, bindingData)
            {
                ReturnValueProvider = returnValueProvider
            };
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new TriggerParameterDescriptor();
        }

        private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
        {
            var result = _parameterExpression is null
                ? new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
                : _parameterExpression.Properties().ToDictionary(property => property.Name, _ => typeof(string));
            result["$return"] = typeof(object).MakeByRefType();
            return result;
        }

        private async Task<Dictionary<string, object>> GetBindingData(JObject trigger)
        {
            // Use _parameterExpression {parameter_name: index} to extract string
            // value from Ignite parameters (assume that they are object[])
            var result = new Dictionary<string, object>();
            if (_parameterExpression != null)
            {
                // FIXME: DRY this code
                object?[] parameters;

                if (trigger.ContainsKey("Call"))
                {
                    var callsCache = _ignite.GetCache<string, CallData>("calls");
                    var callData = await callsCache.GetAsync((string)trigger["Call"]!);
                    parameters = callData.Parameters!;
                }
                else
                {
                    var streamsCache = _ignite.GetCache<string, StreamData>("streams");
                    var streamData = await streamsCache.GetAsync((string)trigger["Stream"]!);
                    parameters = streamData.Parameters!;
                }

                foreach (var property in _parameterExpression.Properties())
                {
                    if (property.Value.Type == JTokenType.Integer)
                    {
                        result[property.Name] = parameters[(int)property.Value!]!;
                    }
                }
            }
            return result;
        }
    }
}