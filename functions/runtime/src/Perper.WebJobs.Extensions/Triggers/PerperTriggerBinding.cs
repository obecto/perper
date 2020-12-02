using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Services;

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
            var trigger = (JObject)value;

            var instanceData = _services.GetRequiredService<PerperInstanceData>();
            await instanceData.SetTriggerValue(trigger);


            var valueProvider = new PerperTriggerValueProvider(trigger, _parameter, instanceData);
            var returnValueProvider = new PerperTriggerValueBinder(trigger, _ignite, _services.GetRequiredService<PerperBinarySerializer>(), _logger);
            var bindingData = await GetBindingData(instanceData, trigger);

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

        private Task<Dictionary<string, object>> GetBindingData(PerperInstanceData instanceData, JObject trigger)
        {
            var result = new Dictionary<string, object>();

            if (_parameterExpression != null)
            {
                foreach (var property in _parameterExpression.Properties())
                {
                    if (property.Value.Type == JTokenType.Integer)
                    {
                        result[property.Name] = instanceData.GetParameters<object[]>()[(int)property.Value!]!;
                    }
                }
            }

            return Task.FromResult(result);
        }
    }
}