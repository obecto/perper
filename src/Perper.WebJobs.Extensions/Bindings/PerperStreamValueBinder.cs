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

        public Task<object> GetValueAsync()
        {
            var taskCompletionSource = new TaskCompletionSource<object>();
            if (Type == typeof(IPerperStream<>))
            {
                var perperStreamType = typeof(PerperStream<>).MakeGenericType(Type.GenericTypeArguments[0]);
                var perperStream = Activator.CreateInstance(perperStreamType,
                    _context.GetInput(_attribute.FunctionName), _attribute.ParameterName);
                taskCompletionSource.SetResult(perperStream);
            }
            else
            {
                var binaryObject = _context.GetBinaryObject(_attribute.FunctionName);
                taskCompletionSource.SetResult(binaryObject.GetField<object>(_attribute.ParameterName));
            }

            return taskCompletionSource.Task;
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        
        public string ToInvokeString()
        {
            //TODO: Research usage?
            return string.Empty;
        }
    }
}