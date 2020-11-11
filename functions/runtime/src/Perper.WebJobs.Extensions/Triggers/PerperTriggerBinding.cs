using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBinding : ITriggerBinding
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly ILogger _logger;

        private readonly JObject? _parameterExpression;

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; }
        public Type TriggerValueType { get; } = typeof(JObject);

        public PerperTriggerBinding(PerperTriggerAttribute attribute,
            FabricService fabric, IIgniteClient ignite, ILogger logger)
        {
            _fabric = fabric;
            _ignite = ignite;
            _logger = logger;

            _parameterExpression = attribute.ParameterExpression is null
                ? null
                : JObject.Parse(attribute.ParameterExpression);

            BindingDataContract = CreateBindingDataContract();
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult<IListener>(new PerperTriggerListener(
                _fabric.GetNotifications(context.Descriptor.ShortName).Select(x => x.Item2) /* FIXME: ConsumeNotification */, context.Executor, _logger));
        }

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var trigger = (JObject) value;
            return new TriggerData(await GetBindingData(trigger))
            {
                ReturnValueProvider = new PerperTriggerValueBinder(trigger, _ignite, _logger)
            };
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new TriggerParameterDescriptor();
        }

        private IReadOnlyDictionary<string, Type> CreateBindingDataContract()
        {
            var result = _parameterExpression is null
                ? new Dictionary<string, Type>()
                : _parameterExpression.Properties().ToDictionary(property => property.Name, _ => typeof(string));
            result["$return"] = typeof(object).MakeByRefType();
            return result;
        }

        private Task<Dictionary<string, object>> GetBindingData(JObject trigger)
        {
            // Use _parameterExpression {parameter_name: index} to extract string
            // value from Ignite parameters (assume that they are object[])
            throw new NotImplementedException();
        }
    }
}