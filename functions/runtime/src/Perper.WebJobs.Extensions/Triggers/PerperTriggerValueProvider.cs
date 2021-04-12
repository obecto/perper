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
        public Type Type => typeof(PerperTriggerValue);

        public PerperTriggerValueProvider(PerperTriggerValue triggerValue)
        {
            _triggerValue = triggerValue;
        }

        public Task<object?> GetValueAsync()
        {
            Console.WriteLine($"{_triggerValue}");
            return Task.FromResult<object?>(_triggerValue);
        }

        public string ToInvokeString()
        {
            return _triggerValue.ToString()!;
        }
    }
}