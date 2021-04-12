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
        private readonly PerperTriggerValue _triggerValue;
        private readonly Type _parameterType;

        public Type Type => typeof(PerperTriggerValue);

        public PerperTriggerValueProvider(PerperTriggerValue triggerValue, Type parameterType)
        {
            _triggerValue = triggerValue;
            _parameterType = parameterType;
        }

        public Task<object?> GetValueAsync()
        {
            if(_parameterType == typeof(string))
            {
                return Task.FromResult<object?>(JObject.FromObject(_triggerValue).ToString());
            }
            return Task.FromResult<object?>(_triggerValue);
        }

        public string ToInvokeString()
        {
            return _triggerValue.ToString()!;
        }
    }
}