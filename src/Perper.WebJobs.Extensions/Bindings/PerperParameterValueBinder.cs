using System;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Perper.Protocol.Header;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperParameterValueBinder : IValueBinder
    {
        private readonly PerperFabricContext _context;
        private readonly PerperAttribute _attribute;
        private readonly IBinary _binary;

        public Type Type { get; }

        public PerperParameterValueBinder(PerperFabricContext context, PerperAttribute attribute, Type type, IBinary binary)
        {
            _context = context;
            _attribute = attribute;
            _binary = binary;

            Type = type;
        }

        public async Task<object> GetValueAsync()
        {
            var input = await _context.GetInput(_attribute.Stream);
            var streamObject = await input.GetStreamObject(default);
            if (streamObject.HasField(_attribute.Parameter))
            {
                return streamObject.GetField<object>(_attribute.Parameter);
            }

            return Activator.CreateInstance(Type);
        }

        public async Task SetValueAsync(object value, CancellationToken cancellationToken)
        {
            var input = await _context.GetInput(_attribute.Stream);
            var streamObject = await input.GetStreamObject(default);

            object header;
            if (streamObject.HasField(_attribute.Parameter))
            {
                header = new StateHeader(_attribute.Parameter);
            }
            else
            {
                header = new WorkerHeader(true);
            }

            var builder = _binary.GetBuilder(header.ToString());
            builder.SetField(_attribute.Parameter, value);
            var binaryObject = builder.Build();

            var output = _context.GetOutput(_attribute.Stream);
            await output.AddAsync(binaryObject);
        }

        public string ToInvokeString()
        {
            //TODO: Research usage?
            return string.Empty;
        }
    }
}