using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
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

            if (Type == typeof(IAsyncEnumerable<>))
            {
                var streamGeneratorMethod = typeof(PerperFabricInput).GetMethod(nameof(input.GetStream));
                var streamGenerator = streamGeneratorMethod?.MakeGenericMethod(Type.GenericTypeArguments[0]);
                var stream = streamGenerator?.Invoke(input, new object[] {_attribute.ParameterName});
                return stream;
            }

            var streamObject = await input.GetStreamObject(default);
            return streamObject.GetField<object>(_attribute.ParameterName);
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