using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Services;
using ITuple = System.Runtime.CompilerServices.ITuple;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValueProvider : IValueProvider
    {
        private readonly JObject _trigger;
        private readonly PerperInstanceData _instance;

        public Type Type { get; }

        public PerperTriggerValueProvider(JObject trigger, Type type, PerperInstanceData instance)
        {
            _trigger = trigger;
            Type = type;
            _instance = instance;
        }

        public Task<object?> GetValueAsync()
        {
            var parameters = _instance.InstanceData.Parameters;

            object? result;

            if (Type.IsAssignableFrom(typeof(object[])))
            {
                result = (object?) parameters;
            }
            else if (typeof(ITuple).IsAssignableFrom(Type))
            {
                result = Activator.CreateInstance(Type, parameters);
            }
            else
            {
                result = parameters?[0];
            }

            return Task.FromResult(result);
        }

        public string ToInvokeString()
        {
            return _trigger.ToString()!;
        }
    }
}