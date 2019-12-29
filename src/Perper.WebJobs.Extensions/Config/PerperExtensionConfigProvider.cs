using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Triggers;

namespace Perper.WebJobs.Extensions.Config
{
    [Extension("Perper")]
    public class PerperExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly IPerperFabricContext _fabricContext;

        public PerperExtensionConfigProvider(IPerperFabricContext fabricContext)
        {
            _fabricContext = fabricContext;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var bindingRule = context.AddBindingRule<PerperAttribute>();
            bindingRule.BindToValueProvider<OpenType>((a, t) =>
                Task.FromResult<IValueBinder>(new PerperValueBinder(_fabricContext, a, t)));
            
            var streamBindingRule = context.AddBindingRule<PerperStreamAttribute>();
            streamBindingRule.BindToValueProvider<IAsyncEnumerable<OpenType>>((a, t) =>
                Task.FromResult<IValueBinder>(new PerperStreamValueBinder(_fabricContext, a, t)));
            streamBindingRule.BindToCollector<OpenType>(typeof(PerperStreamConverter<>), _fabricContext);
            
            var streamTriggerBindingRule = context.AddBindingRule<PerperStreamTriggerAttribute>();
            streamTriggerBindingRule.BindToTrigger(new PerperTriggerBindingProvider<PerperStreamTriggerAttribute>(_fabricContext));

            var workerTriggerBindingRule = context.AddBindingRule<PerperWorkerTriggerAttribute>();
            workerTriggerBindingRule.BindToTrigger(new PerperTriggerBindingProvider<PerperWorkerTriggerAttribute>(_fabricContext));
        }
    }
}