using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Perper.Protocol.Header;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Triggers;

namespace Perper.WebJobs.Extensions.Model
{
    public class PerperStreamContext : IPerperStreamContext
    {
        private readonly PerperFabricInput _input;
        private readonly PerperFabricOutput _output;
        private readonly IBinary _binary;

        public PerperStreamContext(PerperFabricInput input, PerperFabricOutput output, IBinary binary)
        {
            _input = input;
            _output = output;
            _binary = binary;
        }

        public async Task CallStreamAction(string name, object parameters)
        {
            await _output.AddAsync(CreateProtocolObject(new StreamHeader(name, StreamKind.Sink), parameters));
        }

        public async Task<IPerperStreamHandle> CallStreamFunction(string name, object parameters)
        {
            var header = new StreamHeader(name, StreamKind.Pipe);
            await _output.AddAsync(CreateProtocolObject(header, parameters));
            return new PerperStreamHandle(header);
        }

        public async Task<T> CallWorkerFunction<T>(object parameters)
        {
            await _output.AddAsync(CreateProtocolObject(new WorkerHeader(false), parameters));
            var resultBinary = await _input.GetWorkerResult();
            var result = resultBinary.Deserialize<T>();
            return result;
        }

        private IBinaryObject CreateProtocolObject(object header, object parameters)
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