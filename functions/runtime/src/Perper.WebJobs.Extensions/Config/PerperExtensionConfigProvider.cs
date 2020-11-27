using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Triggers;

namespace Perper.WebJobs.Extensions.Config
{
    [Extension("Perper")]
    public class PerperExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly IServiceProvider _services;
        private readonly ILogger _logger;

        public PerperExtensionConfigProvider(FabricService fabric, IIgniteClient ignite, IServiceProvider services, ILogger<PerperExtensionConfigProvider> logger)
        {
            _fabric = fabric;
            _ignite = ignite;
            _services = services;
            _logger = logger;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<PerperAttribute>();
            rule.BindToCollector<OpenType>(attribute => new PerperCollector<OpenType>());

            var triggerRule = context.AddBindingRule<PerperTriggerAttribute>();
            triggerRule.BindToTrigger(new PerperTriggerBindingProvider(_fabric, _ignite, _services, _logger));

            context.AddConverter<JObject, DirectInvokeString>((src, attribute, bindingContext) =>
            {
                return Task.FromResult<DirectInvokeString>(new DirectInvokeString(src.ToString(Formatting.None)));
            });
        }
    }
}