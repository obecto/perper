using System.Reflection;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly PerperFabricContext _fabricContext;
        private readonly IBinary _binary;

        public PerperStreamTriggerBindingProvider(PerperFabricContext fabricContext, IBinary binary)
        {
            _fabricContext = fabricContext;
            _binary = binary;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var triggerAttribute = context.Parameter.GetCustomAttribute<PerperStreamTriggerAttribute>(inherit: false);
            if (triggerAttribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            return Task.FromResult<ITriggerBinding>(new PerperStreamTriggerBinding(_fabricContext, _binary));
        }
    }
}