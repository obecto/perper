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
                nameof(PerperStreamTriggerAttribute) => await data.FetchStreamParameter<object>(_attribute.Parameter),
                nameof(PerperWorkerTriggerAttribute) => await data.FetchWorkerParameterAsync<object>(_attribute.Parameter),
                _ => throw new ArgumentException()
            };
        }

        public async Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            var data = _context.GetData(_attribute.Stream);
            switch (_attribute.TriggerAttribute)
            {
                case nameof(PerperStreamTriggerAttribute):
                    await data.UpdateStreamParameterAsync(_attribute.Parameter, value);
                    break;
                case nameof(PerperWorkerTriggerAttribute):
                    await data.SubmitWorkerResultAsync(value);
                    break;
                default: throw new ArgumentException();
            }
        }

        public string ToInvokeString()
        {
            return $"{_attribute.Stream}/{_attribute.Parameter}";
        }
    }
}