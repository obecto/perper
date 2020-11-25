using System;
using System.Reflection;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly FabricService _fabric;
        private readonly IIgniteClient _ignite;
        private readonly ILogger _logger;

        public PerperTriggerBindingProvider(FabricService fabric, IIgniteClient ignite, ILogger logger)
        {
            _fabric = fabric;
            _ignite = ignite;
            _logger = logger;
        }

        public Task<ITriggerBinding?> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var attribute = context.Parameter.GetCustomAttribute<PerperTriggerAttribute>(false);
            return Task.FromResult<ITriggerBinding?>(attribute switch
            {
                null => null,
                _ => new PerperTriggerBinding(attribute, _fabric, _ignite, _logger)
            });
        }
    }
}