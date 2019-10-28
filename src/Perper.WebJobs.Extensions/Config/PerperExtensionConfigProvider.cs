using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Perper.WebJobs.Extensions.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Triggers;

namespace Perper.WebJobs.Extensions.Config
{
    [Extension("Perper")]
    public class PerperExtensionConfigProvider : IExtensionConfigProvider
    {
        public PerperExtensionConfigProvider(/*DI*/)
        {
            
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var bindingAttributeBindingRule = context.AddBindingRule<StreamAttribute>();
            bindingAttributeBindingRule.BindToValueProvider(delegate(StreamAttribute attribute, Type type)
            {
                var valueBinderType = typeof(StreamValueBinder<>).MakeGenericType(type);
                var valueBinder = (IValueBinder) Activator.CreateInstance(valueBinderType, attribute);
                return Task.FromResult(valueBinder);
            });

            var triggerAttributeBindingRule = context.AddBindingRule<StreamTriggerAttribute>();
            triggerAttributeBindingRule.BindToTrigger<StreamContext>(new StreamTriggerBindingProvider());
        }
    }
}