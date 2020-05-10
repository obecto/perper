using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private readonly PerperStreamAttribute _attribute;

        public Type Type { get; }

        public PerperStreamValueBinder(IPerperFabricContext context, PerperStreamAttribute attribute, Type type)
        {
            _context = context;
            _attribute = attribute;

            Type = type;
        }

        public Task<object> GetValueAsync()
        {
            if (Type == typeof(string))
            {
                return Task.FromResult((object)JsonSerializer.Serialize(new {_attribute.Stream}));
            }
            
            var streamType = typeof(PerperStreamAsyncEnumerable<>).MakeGenericType(Type.GenericTypeArguments[0]);
            var stream = Activator.CreateInstance(streamType, 
                _attribute.Stream, _attribute.Delegate, _attribute.Parameter, _context);
            return Task.FromResult(stream!);
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