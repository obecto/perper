using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperTriggerValueProvider : IValueProvider
    {
        private readonly object _value;

        public PerperTriggerValueProvider(object value)
        {
            _value = value;

            Type = value.GetType();
        }
        
        public Task<object> GetValueAsync()
        {
            return Task.FromResult(_value);
        }

        public string ToInvokeString()
        {
            return "context";
        }

        public Type Type { get; }
    }
}