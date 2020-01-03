using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBindingProvider<TAttribute> : ITriggerBindingProvider where TAttribute : Attribute
    {
        private readonly IPerperFabricContext _fabricContext;
        private readonly ILogger _logger;

        public PerperTriggerBindingProvider(IPerperFabricContext fabricContext, ILogger logger)
        {
            _fabricContext = fabricContext;
            _logger = logger;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var attribute = context.Parameter.GetCustomAttribute<TAttribute>(false);
            return Task.FromResult<ITriggerBinding>(attribute == null
                ? null
                : new PerperTriggerBinding(attribute, _fabricContext, _logger));
        }
    }
}