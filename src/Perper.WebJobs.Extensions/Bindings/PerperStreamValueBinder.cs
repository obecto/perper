using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.Protocol.Cache;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Model;

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

        public async Task<object> GetValueAsync()
        {
            if (Type == typeof(string))
            {
                return (object)JsonSerializer.Serialize(new {_attribute.Stream});
            }

            if (Type == typeof(IPerperStream))
            {
                var data = _context.GetData(_attribute.Stream);
                //data.FetchWorkerParameterAsync<object[]>(_attribute.Parameter).ContinueWith(x => (object)data.GetStream((x.Result[0] as IBinaryObject).Deserialize<StreamRef>()));

                var result = _attribute.TriggerAttribute switch
                {
                    nameof(PerperModuleTriggerAttribute) => await data.FetchWorkerParameterAsync<object[]>(_attribute.Worker, _attribute.Parameter),
                    _ => throw new ArgumentException()
                };

                return data.GetStream((result[0] as IBinaryObject).Deserialize<StreamRef>());
            }

            var streamType = typeof(PerperStreamAsyncEnumerable<>).MakeGenericType(Type.GenericTypeArguments[0]);
            var stream = Activator.CreateInstance(streamType,
                _attribute.Stream, _attribute.Delegate, _attribute.Parameter, _context);
            return stream;
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