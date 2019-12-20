using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBindingProvider<TAttribute> : ITriggerBindingProvider where TAttribute : Attribute
    {
        private readonly IPerperFabricContext _fabricContext;

        public PerperTriggerBindingProvider(IPerperFabricContext fabricContext)
        {
            _fabricContext = fabricContext;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var attribute = context.Parameter.GetCustomAttribute<TAttribute>(inherit: false);
            return Task.FromResult<ITriggerBinding>(attribute == null
                ? null
                : new PerperTriggerBinding(_fabricContext, attribute, context.Parameter.Name));
        }
    }
}