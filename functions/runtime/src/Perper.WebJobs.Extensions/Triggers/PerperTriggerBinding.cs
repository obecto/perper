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
using Perper.WebJobs.Extensions.Model;
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
                _fabric, context.Descriptor.ShortName, _ignite, context.Executor, _logger));
        }

        class DummyClass {
            public IContext Context { get; }
            public IServiceProvider Services { get; }
            public DummyClass(IContext context, IServiceProvider services) {
                Context = context;
                Services = services;
            }
        }

        public async Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var trigger = (JObject) value;

            // HACK: Apparently context.FunctionContext.InstanceServices is never set by the WebJobs SDK.
            // So we create a dummy object and extract the wanted services through it.
            var dummy = context.FunctionContext.CreateObjectInstance<DummyClass>();

            // HACK: Also, we cannot access the services in the async converter for JObject.
            // So we just plug them in here; see PerperExtensionConfigProvider
            trigger.AddAnnotation(dummy.Services);

            await ((Context) dummy.Context).SetTriggerValue(trigger);

            var returnProvider = new PerperTriggerValueBinder(trigger, _ignite, _logger);
            return new TriggerData(await GetBindingData(trigger))
            {
                ReturnValueProvider = returnProvider
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
            result["stream"] = typeof(string); // HACK!
            return result;
        }

        private Task<Dictionary<string, object>> GetBindingData(JObject trigger)
        {
            // Use _parameterExpression {parameter_name: index} to extract string
            // value from Ignite parameters (assume that they are object[])
            var result = _parameterExpression is null
                ? new Dictionary<string, object>()
                : _parameterExpression.Properties().ToDictionary(property => property.Name, (Func<JProperty, object>)(_ => throw new System.Exception("123")));
            result["stream"] = trigger.ToString(); // HACK!
            return Task.FromResult(result);
        }
    }
}