using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Microsoft.Extensions.Logging;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBinding : ITriggerBinding
    {
        private readonly Attribute _attribute;
        private readonly IPerperFabricContext _fabricContext;
        private readonly ILogger _logger;
        private readonly PerperWorkerTriggerValueConverter _workerTriggerValueConverter;
        public Type TriggerValueType { get; }

        public PerperTriggerBinding(Attribute attribute, Type triggerValueType,
            IPerperFabricContext fabricContext, ILogger logger)
        {
            _attribute = attribute;
            _fabricContext = fabricContext;
            _logger = logger;
            _workerTriggerValueConverter = new PerperWorkerTriggerValueConverter(triggerValueType);

            TriggerValueType = triggerValueType;
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult<IListener>(_attribute switch
            {
                PerperStreamTriggerAttribute streamAttribute => new PerperStreamListener(streamAttribute,
                    context.Descriptor.FullName, context.Executor, _fabricContext, _logger),
                PerperWorkerTriggerAttribute workerAttribute => new PerperWorkerListener(workerAttribute,
                    context.Descriptor.FullName, _workerTriggerValueConverter, context.Executor, _fabricContext),
                PerperModuleTriggerAttribute moduleAttribute => new PerperModuleListener(moduleAttribute,
                    context.Descriptor.ShortName, context.Executor, _fabricContext),
                _ => throw new ArgumentException()
            });
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            var (streamName, workerName, delegateName, triggerAttributeName) = GetTriggerData(value);
            return Task.FromResult<ITriggerData>(new TriggerData(new PerperTriggerValueProvider(value),
                new Dictionary<string, object>
                {
                    {"stream", streamName},
                    {"worker", workerName},
                    {"delegate", delegateName},
                    {"triggerAttribute", triggerAttributeName}
                }));
        }

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
        {
            {"stream", typeof(string)},
            {"worker", typeof(string)},
            {"delegate", typeof(string)},
            {"triggerAttribute", typeof(string)}
        };

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }

        private (string, string, string, string) GetTriggerData(object value)
        {
            switch (_attribute)
            {
                case PerperStreamTriggerAttribute _:
                {
                    var context = (PerperStreamContext) value;
                    return (context.StreamName, null, context.DelegateName, nameof(PerperStreamTriggerAttribute));
                }
                case PerperWorkerTriggerAttribute _:
                {
                    var context = _workerTriggerValueConverter.ConvertBack(value);
                    return (context.StreamName, context.WorkerName, null, nameof(PerperWorkerTriggerAttribute));
                }
                case PerperModuleTriggerAttribute _:
                {
                    var context = (PerperModuleContext) value;
                    return (context.StreamName, null, context.DelegateName, nameof(PerperModuleTriggerAttribute));
                }
                default:
                    throw new ArgumentException();
            }
        }
    }
}