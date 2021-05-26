using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Newtonsoft.Json.Linq;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValueProvider : IValueProvider
    {
        private readonly JObject _trigger;
        private readonly PerperInstanceData _instance;

        public Type Type { get; }

        public PerperTriggerValueProvider(JObject trigger, ParameterInfo parameter, PerperInstanceData instance)
        {
            Type = parameter.ParameterType;
            _trigger = trigger;
            _instance = instance;
        }

        public Task<object?> GetValueAsync()
        {
            // WARNING: This BREAKS if you are trying to call a dotnet function from python
            // TODO: Fix / Get Converters working.
            var value = _instance.GetParameters(Type) ?? ToInvokeString();

            return Task.FromResult<object?>(value);
        }

        public string ToInvokeString() => _trigger.ToString()!;
    }
}