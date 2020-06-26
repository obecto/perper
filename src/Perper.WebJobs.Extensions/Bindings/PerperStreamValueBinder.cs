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

        public Task<object> GetValueAsync()
        {
            if (Type == typeof(string))
            {
                return Task.FromResult((object)JsonSerializer.Serialize(new {_attribute.Stream}));
            }

            if (Type == typeof(IPerperStream))
            {
                var data = _context.GetData(_attribute.Stream);
                return data.FetchStreamParameterAsync<object[]>(_attribute.Parameter).ContinueWith(x => (object)data.GetStream((x.Result[0] as IBinaryObject).Deserialize<StreamRef>()));
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