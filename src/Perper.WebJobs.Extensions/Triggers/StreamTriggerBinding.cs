using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class StreamTriggerBinding : ITriggerBinding
    {
        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            throw new NotImplementedException();
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult<IListener>(new StreamListener(context.Executor));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }

        public Type TriggerValueType => typeof(StreamContext);
        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>();
    }
}