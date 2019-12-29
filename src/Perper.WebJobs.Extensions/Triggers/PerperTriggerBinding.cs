using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBinding : ITriggerBinding
    {
        private readonly Attribute _attribute;
        private readonly IPerperFabricContext _fabricContext;
        public Type TriggerValueType { get; }

        public PerperTriggerBinding(Attribute attribute, IPerperFabricContext fabricContext)
        {
            _attribute = attribute;
            _fabricContext = fabricContext;

            TriggerValueType = GetTriggerValueType();
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult<IListener>(_attribute switch
            {
                PerperStreamTriggerAttribute streamAttribute => new PerperStreamListener(streamAttribute, 
                    context.Descriptor.ShortName, context.Executor, _fabricContext),
                PerperWorkerTriggerAttribute workerAttribute => new PerperWorkerListener(workerAttribute, 
                    context.Executor, _fabricContext),
                _ => throw new ArgumentException()
            });
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var (streamName, delegateName, triggerAttributeName) = GetTriggerData(value);
            return Task.FromResult<ITriggerData>(new TriggerData(new PerperTriggerValueProvider(value),
                new Dictionary<string, object>
                {
                    {"stream", streamName},
                    {"delegate", delegateName},
                    {"triggerAttribute", triggerAttributeName}
                }));
        }

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
        {
            {"stream", typeof(string)},
            {"delegate", typeof(string)},
            {"triggerAttribute", typeof(string)}
        };

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }

        private Type GetTriggerValueType()
        {
            return _attribute switch
            {
                PerperStreamTriggerAttribute _ => typeof(PerperStreamContext),
                PerperWorkerTriggerAttribute _ => typeof(PerperWorkerContext),
                _ => throw new ArgumentException()
            };
        }

        private (string, string, string) GetTriggerData(object value)
        {
            return _attribute switch
            {
                PerperStreamTriggerAttribute _ => (((PerperStreamContext) value).StreamName,
                    ((PerperStreamContext) value).DelegateName, nameof(PerperStreamTriggerAttribute)),
                PerperWorkerTriggerAttribute workerAttribute => (((PerperWorkerContext) value).StreamName,
                    workerAttribute.StreamDelegate, nameof(PerperWorkerTriggerAttribute)),
                _ => throw new ArgumentException()
            };
        }
    }
}