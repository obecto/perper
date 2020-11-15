using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Triggers;

namespace Perper.WebJobs.Extensions.Config
{
    [Extension("Perper")]
    public class PerperExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly ILogger _logger;

        public PerperExtensionConfigProvider(FabricService fabric, IIgniteClient ignite, ILogger logger)
        {
            _fabric = fabric;
            _ignite = ignite;
            _logger = logger;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<PerperAttribute>();
            rule.BindToCollector<OpenType>(attribute => new PerperCollector<OpenType>());

            var triggerRule = context.AddBindingRule<PerperTriggerAttribute>();
            triggerRule.BindToTrigger<OpenType>(new PerperTriggerBindingProvider(_fabric, _ignite, _logger));

            context.AddOpenConverter<JObject, OpenType>((src, attribute, bindingContext) =>
            {
                var perperContext = (Context) bindingContext.FunctionContext.InstanceServices.GetService(typeof(IContext));
                perperContext.TriggerValue = (JObject) src;

                //Use _ignite to get the parameters
                return Task.FromResult<object>(default!);
            });

            context.AddConverter<JObject, string>((src, attribute, bindingContext) => throw new NotImplementedException());
        }
    }
}