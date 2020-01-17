using System;
using System.Threading;
using System.Threading.Tasks;
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
            if (_attribute.Parameter == "$return")
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