using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamTriggerBindingProvider : ITriggerBindingProvider
    {
        public PerperStreamTriggerBindingProvider(/*DI*/)
        {
            
        }
        
        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var triggerAttribute = context.Parameter.GetCustomAttribute<PerperStreamTriggerAttribute>(inherit: false);
            if (triggerAttribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            return Task.FromResult<ITriggerBinding>(new PerperStreamTriggerBinding());
        }
    }
}