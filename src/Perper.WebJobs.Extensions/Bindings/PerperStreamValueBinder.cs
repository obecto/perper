using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamValueBinder : IValueBinder
    {
        private readonly PerperFabricContext _context;
        private readonly PerperAttribute _attribute;

        public Type Type { get; }

        public PerperStreamValueBinder(PerperFabricContext context, PerperAttribute attribute, Type type)
        {
            _context = context;
            _attribute = attribute;

            Type = type;
        }

        public async Task<object> GetValueAsync()
        {
            var input = await _context.GetInput(_attribute.Stream);

            var streamGeneratorMethod = typeof(PerperFabricInput).GetMethod(nameof(input.GetStream));
            var streamGenerator = streamGeneratorMethod?.MakeGenericMethod(Type.GenericTypeArguments[0]);
            var stream = streamGenerator?.Invoke(input, new object[] {_attribute.Parameter});
            return stream;
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public string ToInvokeString()
        {
            //TODO: Research usage?
            return string.Empty;
        }
    }
}