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
            var bindingAttributeBindingRule = context.AddBindingRule<PerperStreamAttribute>();
            bindingAttributeBindingRule.BindToValueProvider((attribute, type) =>
                Task.FromResult<IValueBinder>(new PerperStreamValueBinder(_fabricContext, attribute, type)));
            bindingAttributeBindingRule.BindToCollector(attribute =>
                new PerperStreamAsyncCollector<OpenType>(_fabricContext.GetOutput(attribute.FunctionName), _binary));

            var triggerAttributeBindingRule = context.AddBindingRule<PerperStreamTriggerAttribute>();
            triggerAttributeBindingRule.BindToTrigger<IPerperStreamContext>(
                new PerperStreamTriggerBindingProvider(_fabricContext, _binary));
        }
    }
}