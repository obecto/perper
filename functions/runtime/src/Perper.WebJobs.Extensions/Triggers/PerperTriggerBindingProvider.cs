using System;
using System.Reflection;
using System.Threading.Tasks;

using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.DependencyInjection;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly IServiceProvider _services;

        public PerperTriggerBindingProvider(IServiceProvider services) => _services = services;

        public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var attribute = context.Parameter.GetCustomAttribute<PerperTriggerAttribute>(false);
            return Task.FromResult<ITriggerBinding?>(attribute switch
            {
                null => null,
                _ => ActivatorUtilities.CreateInstance<PerperTriggerBinding>(_services, context.Parameter, attribute)
            });
        }
    }
}