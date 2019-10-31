using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamValueBinder<T> : IValueBinder where T : class
    {
        public Type Type => typeof(T);

        public PerperStreamValueBinder(PerperStreamAttribute attribute)
        {
            
        }

        public Task<object> GetValueAsync()
        {
            throw new NotImplementedException();
        }
        
        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        public string ToInvokeString()
        {
            throw new NotImplementedException();
        }
    }
}