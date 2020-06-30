using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Model;

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
            if (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(IAsyncCollector<>))
            {
                if (_attribute.TriggerAttribute == nameof(PerperStreamTriggerAttribute))
                {
                    var collectorType = typeof(PerperStreamAsyncCollector<>).MakeGenericType(Type.GenericTypeArguments);
                    return Activator.CreateInstance(collectorType, _attribute.Stream, _context)!;
                }
                else
                {
                    var collectorType = typeof(PerperWorkerAsyncCollector<>).MakeGenericType(Type.GenericTypeArguments);
                    return Activator.CreateInstance(collectorType, _attribute.Stream, _attribute.Worker, _context)!;
                }
            }

            if (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
            {
                var streamType = typeof(PerperStreamAsyncEnumerable<>).MakeGenericType(Type.GenericTypeArguments[0]);
                return Activator.CreateInstance(streamType, _attribute.Stream, _attribute.Delegate, _attribute.Parameter, _context)!;
            }

            var data = _context.GetData(_attribute.Stream);
            var result = _attribute.TriggerAttribute switch
            {
                nameof(PerperStreamTriggerAttribute) => await data.FetchStreamParameterAsync<object>(_attribute
                    .Parameter),
                nameof(PerperWorkerTriggerAttribute) => await data.FetchWorkerParameterAsync<object>(_attribute.Worker,
                    _attribute.Parameter),
                nameof(PerperModuleTriggerAttribute) => await data.FetchWorkerParameterAsync<object>(_attribute.Worker, _attribute.Parameter),
                _ => throw new ArgumentException()
            };

            if (Type == typeof(IPerperStream[]) && result is object[] binaryObjects)
            {
                result = binaryObjects.OfType<IBinaryObject>().Select(x => x.Deserialize<PerperFabricStream>()).ToArray();
            }

            return result;
        }

        public async Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            if (_attribute.Parameter == "$return" && !TypeIsAsyncCollector())
            {
                var data = _context.GetData(_attribute.Stream);
                await data.SubmitWorkerResultAsync(_attribute.Worker, value);
            }
        }

        public string ToInvokeString()
        {
            return $"{_attribute.Stream}/{_attribute.Parameter}";
        }

        private bool TypeIsAsyncCollector()
        {
            return Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(IAsyncCollector<>);
        }
    }
}