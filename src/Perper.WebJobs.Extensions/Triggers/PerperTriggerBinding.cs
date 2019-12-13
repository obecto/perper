using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Executors;
using Microsoft.Azure.WebJobs.Host.Listeners;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Azure.WebJobs.Host.Triggers;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerBinding : ITriggerBinding
    {
        private readonly PerperFunctionType _functionType;
        private readonly string _stream;
        private readonly string _parameterName;
        private readonly PerperFabricContext _fabricContext;
        private readonly IBinary _binary;

        private readonly Func<string, string, PerperFabricContext, IBinary, ITriggeredFunctionExecutor, IListener>
            _listenerFactory;

        public PerperTriggerBinding(PerperFunctionType functionType, string stream, string parameterName,
            Type parameterType, PerperFabricContext fabricContext, IBinary binary,
            Func<string, string, PerperFabricContext, IBinary, ITriggeredFunctionExecutor, IListener> listenerFactory)
        {
            _functionType = functionType;
            _stream = stream;
            _parameterName = parameterName;
            TriggerValueType = parameterType;
            _fabricContext = fabricContext;
            _binary = binary;
            _listenerFactory = listenerFactory;
        }

        public Task<ITriggerData> BindAsync(object value, ValueBindingContext context)
        {
            return Task.FromResult<ITriggerData>(new TriggerData(new Dictionary<string, object>
            {
                {"stream", _stream},
                {"functionType", _functionType.ToString()}
            }));
        }

        public Task<IListener> CreateListenerAsync(ListenerFactoryContext context)
        {
            return Task.FromResult(_listenerFactory(_stream, _parameterName, _fabricContext,
                _binary, context.Executor));
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return new ParameterDescriptor();
        }

        public Type TriggerValueType { get; }

        public IReadOnlyDictionary<string, Type> BindingDataContract { get; } = new Dictionary<string, Type>
        {
            {"stream", typeof(string)},
            {"functionType", typeof(string)}
        };
    }
}