using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamValueBinder : IValueBinder
    {
        private readonly PerperFabricContext _context;
        private readonly PerperStreamAttribute _attribute;

        public Type Type { get; }

        public PerperStreamValueBinder(PerperFabricContext context, PerperStreamAttribute attribute, Type type)
        {
            _context = context;
            _attribute = attribute;
            
            Type = type;
        }

        public async Task<object> GetValueAsync()
        {
            var input = await _context.GetInput(_attribute.FunctionName);

            var taskCompletionSource = new TaskCompletionSource<object>();
            if (Type == typeof(IPerperStream<>))
            {
                var perperStreamType = typeof(PerperStream<>).MakeGenericType(Type.GenericTypeArguments[0]);
                var perperStream = Activator.CreateInstance(perperStreamType, input, _attribute.ParameterName);
                taskCompletionSource.SetResult(perperStream);
            }
            else
            {
                var activationObject = input.GetActivationObject();
                taskCompletionSource.SetResult(activationObject.GetField<object>(_attribute.ParameterName));
            }

            return taskCompletionSource.Task;
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            //TODO: Handle return value?
            throw new NotImplementedException();
        }
        
        public string ToInvokeString()
        {
            //TODO: Research usage?
            return string.Empty;
        }
    }
}