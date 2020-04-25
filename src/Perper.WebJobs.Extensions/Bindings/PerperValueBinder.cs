using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperValueBinder : IValueBinder
    {
        private readonly IPerperFabricContext _context;
        private readonly PerperAttribute _attribute;

        public Type Type { get; }

        public PerperValueBinder(IPerperFabricContext context, PerperAttribute attribute, Type type)
        {
            _context = context;
            _attribute = attribute;

            Type = type;
        }

        public async Task<object> GetValueAsync()
        {
            if (_attribute.Parameter == "$return" && Type.GetGenericTypeDefinition() == typeof(IAsyncCollector<>))
            {
                var collectorType = typeof(PerperWorkerAsyncCollector<>).MakeGenericType(Type.GenericTypeArguments);
                return Activator.CreateInstance(collectorType, _attribute.Stream, _attribute.Worker, _context);
            }
            
            var data = _context.GetData(_attribute.Stream);
            return _attribute.TriggerAttribute switch
            {
                nameof(PerperStreamTriggerAttribute) => await data.FetchStreamParameterAsync<object>(_attribute.Parameter),
                nameof(PerperWorkerTriggerAttribute) => await data.FetchWorkerParameterAsync<object>(_attribute.Worker, _attribute.Parameter),
                _ => throw new ArgumentException()
            };
        }

        public async Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            if (_attribute.Parameter == "$return" && Type.GetGenericTypeDefinition() != typeof(IAsyncCollector<>))
            {
                var data = _context.GetData(_attribute.Stream);
                await data.SubmitWorkerResultAsync(_attribute.Worker, value);
            }
        }

        public string ToInvokeString()
        {
            return $"{_attribute.Stream}/{_attribute.Parameter}";
        }
    }
}