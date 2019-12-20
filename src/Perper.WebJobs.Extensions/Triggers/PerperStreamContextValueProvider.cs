using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamContextValueProvider : IValueProvider
    {
        private readonly object _value;

        public PerperStreamContextValueProvider(object value)
        {
            _value = value;
        }
        
        public Task<object> GetValueAsync()
        {
            return Task.FromResult(_value);
        }

        public string ToInvokeString()
        {
            return "context";
        }

        public Type Type { get; } = typeof(PerperStreamContext);
    }
}