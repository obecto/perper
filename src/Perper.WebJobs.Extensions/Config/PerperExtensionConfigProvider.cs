using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Triggers;

namespace Perper.WebJobs.Extensions.Config
{
    [Extension("Perper")]
    public class PerperExtensionConfigProvider : IExtensionConfigProvider
    {
        private readonly PerperFabricContext _fabricContext;
        private readonly IBinary _binary;

        public PerperExtensionConfigProvider(PerperFabricContext fabricContext, IBinary binary)
        {
            _fabricContext = fabricContext;
            _binary = binary;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var bindingAttributeBindingRule = context.AddBindingRule<PerperAttribute>();

            bindingAttributeBindingRule.BindToValueProvider((a, t) =>
                Task.FromResult<IValueBinder>(new PerperParameterValueBinder(_fabricContext, a, t, _binary)));
            bindingAttributeBindingRule.BindToValueProvider<IAsyncEnumerable<OpenType>>((a, t) =>
                Task.FromResult<IValueBinder>(new PerperStreamValueBinder(_fabricContext, a, t)));

            bindingAttributeBindingRule.BindToCollector(a =>
                new PerperStreamAsyncCollector<OpenType>(_fabricContext.GetOutput(a.Stream), _binary));

            var triggerAttributeBindingRule = context.AddBindingRule<PerperTriggerAttribute>();
            triggerAttributeBindingRule.BindToTrigger<IPerperStreamContext>(
                new PerperTriggerBindingProvider(_fabricContext, _binary,
                    (s, p, f, b, e) => new PerperStreamListener(s, p, f, b, e)));
            triggerAttributeBindingRule.BindToTrigger(
                new PerperTriggerBindingProvider(_fabricContext, _binary,
                    (s, p, f, b, e) => new PerperWorkerListener(s, p, f, b, e)));
        }
    }
}