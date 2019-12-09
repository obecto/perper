using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Perper.Protocol.Header;
using Perper.WebJobs.Extensions.Model;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamContext : IPerperStreamContext
    {
        private readonly PerperFabricOutput _output;
        private readonly IBinary _binary;

        public PerperStreamContext(PerperFabricOutput output, IBinary binary)
        {
            _output = output;
            _binary = binary;
        }

        public async Task CallStreamAction(string name, object parameters)
        {
            await _output.AddAsync(CreateStreamObject(new StreamHeader(name, StreamKind.Sink), parameters));
        }

        public async Task<IPerperStreamHandle> CallStreamFunction<T>(string name, object parameters)
        {
            var header = new StreamHeader(name, StreamKind.Pipe);
            await _output.AddAsync(CreateStreamObject(header, parameters));
            return new PerperStreamHandle(header);
        }

        private IBinaryObject CreateStreamObject(StreamHeader header, object parameters)
        {
            var builder = _binary.GetBuilder(header.ToString());

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(properties);
                if (propertyValue is PerperStreamHandle streamHandle)
                {
                    builder.SetField(propertyInfo.Name, _binary.GetBuilder(streamHandle.Header.ToString()).Build());
                }
                else
                {
                    builder.SetField(propertyInfo.Name, propertyValue);
                }
            }

            return builder.Build();
        }
    }
}