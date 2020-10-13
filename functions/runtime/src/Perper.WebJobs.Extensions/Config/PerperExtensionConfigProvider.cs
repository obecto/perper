using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Triggers;

namespace Perper.WebJobs.Extensions.Config
{
    [Extension("Perper")]
    public class PerperExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IPerperFabricContext _fabricContext;
        private readonly ILogger _logger;

        public PerperExtensionConfigProvider(IPerperFabricContext fabricContext, ILogger<PerperExtensionConfigProvider> logger)
        {
            _fabricContext = fabricContext;
            _logger = logger;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var bindingRule = context.AddBindingRule<PerperAttribute>();
            bindingRule.BindToValueProvider<OpenType>((a, t) =>
                Task.FromResult<IValueBinder>(new PerperValueBinder(_fabricContext, a, t)));

            var streamTriggerBindingRule = context.AddBindingRule<PerperStreamTriggerAttribute>();
            streamTriggerBindingRule.BindToTrigger(new PerperTriggerBindingProvider<PerperStreamTriggerAttribute>(_fabricContext, _logger));

            var workerTriggerBindingRule = context.AddBindingRule<PerperWorkerTriggerAttribute>();
            workerTriggerBindingRule.BindToTrigger(new PerperTriggerBindingProvider<PerperWorkerTriggerAttribute>(_fabricContext, _logger));

            var moduleTriggerBindingRule = context.AddBindingRule<PerperModuleTriggerAttribute>();
            moduleTriggerBindingRule.BindToTrigger(new PerperTriggerBindingProvider<PerperModuleTriggerAttribute>(_fabricContext, _logger));
        }
    }
}