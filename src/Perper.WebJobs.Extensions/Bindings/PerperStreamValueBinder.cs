using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamValueBinder : IValueBinder
    {
        private readonly IPerperFabricContext _context;
        private readonly PerperAttribute _attribute;

        public Type Type { get; }

        public PerperStreamValueBinder(IPerperFabricContext context, PerperAttribute attribute, Type type)
        {
            _context = context;
            _attribute = attribute;

            Type = type;
        }

        public Task<object> GetValueAsync()
        {
            var streamType = typeof(PerperStreamAsyncEnumerable<>).MakeGenericType(Type);
            var stream = Activator.CreateInstance(streamType, _context, _attribute.Stream, _attribute.Parameter);
            return Task.FromResult(stream);
        }

        public Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public string ToInvokeString()
        {
            return $"{_attribute.Stream}/{_attribute.Parameter}";
        }
    }
}