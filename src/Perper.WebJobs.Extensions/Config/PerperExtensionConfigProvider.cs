using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
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
        private readonly PerperFabricContext _fabricContext;
        private readonly IBinary _binary;

        public PerperExtensionConfigProvider(PerperFabricContext fabricContext, IBinary binary)
        {
            _fabricContext = fabricContext;
            _binary = binary;
        }

        public void Initialize(ExtensionConfigContext context)
        {
            var bindingRule = context.AddBindingRule<PerperAttribute>();
            bindingRule.BindToValueProvider<OpenType>((a, t) =>
                Task.FromResult<IValueBinder>(new PerperValueBinder(_fabricContext, a, t, _binary)));
            bindingRule.BindToValueProvider<IAsyncEnumerable<OpenType>>((a, t) =>
                Task.FromResult<IValueBinder>(new PerperStreamValueBinder(_fabricContext, a, t)));
            bindingRule.BindToCollector<OpenType>(typeof(PerperStreamConverter<>), _fabricContext, _binary);

            var streamBindingRule = context.AddBindingRule<PerperStreamAttribute>();
            streamBindingRule.BindToTrigger(
                new PerperTriggerBindingProvider(_fabricContext, _binary,
                    (s, p, f, b, e) => new PerperStreamListener(s, p, f, b, e)));

            var workerBindingRule = context.AddBindingRule<PerperWorkerAttribute>();
            workerBindingRule.BindToTrigger(
                new PerperTriggerBindingProvider(_fabricContext, _binary,
                    (s, p, f, b, e) => new PerperWorkerListener(s, p, f, b, e)));
        }
    }
}