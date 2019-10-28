using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class StreamTriggerBindingProvider : ITriggerBindingProvider
    {
        public StreamTriggerBindingProvider(/*DI*/)
        {
            
        }
        
        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var triggerAttribute = context.Parameter.GetCustomAttribute<StreamTriggerAttribute>(inherit: false);
            if (triggerAttribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }
            return Task.FromResult<ITriggerBinding>(new StreamTriggerBinding());
        }
    }
}