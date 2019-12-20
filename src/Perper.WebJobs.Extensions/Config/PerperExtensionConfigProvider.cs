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
            bindingRule.BindToValueProvider<IAsyncEnumerable<OpenType>>((a, t) =>
                Task.FromResult<IValueBinder>(new PerperStreamValueBinder(_fabricContext, a, t)));
            bindingRule.BindToCollector<OpenType>(typeof(PerperStreamConverter<>), _fabricContext);

            var streamBindingRule = context.AddBindingRule<PerperStreamTriggerAttribute>();
            streamBindingRule.BindToTrigger(new PerperTriggerBindingProvider<PerperStreamTriggerAttribute>(_fabricContext));

            var workerBindingRule = context.AddBindingRule<PerperWorkerTriggerAttribute>();
            workerBindingRule.BindToTrigger(new PerperTriggerBindingProvider<PerperStreamTriggerAttribute>(_fabricContext));
        }
    }
}