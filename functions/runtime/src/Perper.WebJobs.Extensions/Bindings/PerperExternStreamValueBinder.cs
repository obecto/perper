using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Newtonsoft.Json.Linq;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperExternStreamValueBinder : IValueBinder
    {
        public PerperExternStreamValueBinder(string externStreamRef)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetValueAsync()
        {
            throw new NotImplementedException();
        }

        public string ToInvokeString()
        {
            throw new NotImplementedException();
        }

        public Type Type { get; } = typeof(JObject);

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}