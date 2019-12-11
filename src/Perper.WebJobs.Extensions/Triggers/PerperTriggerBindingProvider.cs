using System;
using System.Reflection;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBindingProvider : ITriggerBindingProvider
    {
        private readonly PerperFabricContext _fabricContext;
        private readonly IBinary _binary;
        private readonly Func<string, PerperFabricContext, IBinary, ITriggeredFunctionExecutor, IListener> _listenerFactory;

        public PerperTriggerBindingProvider(PerperFabricContext fabricContext, IBinary binary,
            Func<string, PerperFabricContext, IBinary, ITriggeredFunctionExecutor, IListener> listenerFactory)
        {
            _fabricContext = fabricContext;
            _binary = binary;
            _listenerFactory = listenerFactory;
        }

        public Task<ITriggerBinding> TryCreateAsync(TriggerBindingProviderContext context)
        {
            var triggerAttribute = context.Parameter.GetCustomAttribute<PerperAttribute>(inherit: false);
            if (triggerAttribute == null)
            {
                return Task.FromResult<ITriggerBinding>(null);
            }

            return Task.FromResult<ITriggerBinding>(new PerperStreamTriggerBinding(_fabricContext, _binary, _listenerFactory));
        }
    }
}