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
        private readonly IPerperFabricContext _fabricContext;
        private readonly Attribute _attribute;
        private readonly string _name;

        public Type TriggerValueType { get; }
        
        public PerperTriggerBinding(IPerperFabricContext fabricContext, Attribute attribute, string name)
        {
            _fabricContext = fabricContext;
            _attribute = attribute;
            _name = name;

            TriggerValueType = GetTriggerValueType();
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult<IListener>(_attribute switch
            {
                PerperStreamAttribute streamAttribute => new PerperStreamListener(_fabricContext, streamAttribute, _name, context.Executor),
                PerperWorkerAttribute workerAttribute => new PerperWorkerListener(_fabricContext, workerAttribute, _name, context.Executor),
                _ => throw new ArgumentException()
            });
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var (stream, triggerAttribute) = GetAttributeData();
            return Task.FromResult<ITriggerData>(new TriggerData(new Dictionary<string, object>
            {
                {"stream", stream},
                {"triggerAttribute", triggerAttribute}
            }));
        }
        
        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
        {
            {"stream", typeof(string)},
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
                PerperStreamAttribute _ => typeof(IPerperStreamContext),
                PerperWorkerAttribute _ => typeof(IPerperWorkerContext),
                _ => throw new ArgumentException()
            };
        }

        private (string, string) GetAttributeData()
        {
            return _attribute switch
            {
                PerperStreamAttribute streamAttribute => (streamAttribute.Stream, nameof(PerperStreamAttribute)),
                PerperWorkerAttribute workerAttribute => (workerAttribute.Stream, nameof(PerperWorkerAttribute)),
                _ => throw new ArgumentException()
            };
        }
    }
}