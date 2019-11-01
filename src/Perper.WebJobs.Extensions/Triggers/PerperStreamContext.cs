using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamContext : IPerperStreamContext
    {
        private readonly IAsyncCollector<IBinaryObject> _output;
        private readonly IBinary _binary;

        public PerperStreamContext(IAsyncCollector<IBinaryObject> output, IBinary binary)
        {
            _output = output;
            _binary = binary;
        }

        public async Task CallStreamAction(string funcName, object parameters)
        {
            await _output.AddAsync(CreateStreamBinaryObject(funcName, parameters, "action"));
        }

        public async Task<IPerperStreamHandle> CallStreamFunction(string funcName, object parameters)
        {
            await _output.AddAsync(CreateStreamBinaryObject(funcName, parameters, "function"));
            return new PerperStreamHandleImpl(funcName);
        }

        private IBinaryObject CreateStreamBinaryObject(string funcName, object parameters, string type)
        {
            var builder = _binary.GetBuilder("Perper.Stream");
            builder.SetStringField("funcName", funcName);
            builder.SetStringField("type", type);

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                if (propertyInfo.PropertyType == typeof(IPerperStreamHandle))
                {
                    var (streamReferenceName) = (PerperStreamHandleImpl) propertyInfo.GetValue(parameters);
                    var streamReferenceBuilder = _binary.GetBuilder("Perper.StreamReference");
                    streamReferenceBuilder.SetStringField("funcName", streamReferenceName);
                    builder.SetField(propertyInfo.Name, streamReferenceBuilder.Build());
                }
                else
                {
                    builder.SetField(propertyInfo.Name, propertyInfo.PropertyType);
                }
            }

            return builder.Build();
        }

        private class PerperStreamHandleImpl : Tuple<string>, IPerperStreamHandle
        {
            public PerperStreamHandleImpl(string funcName) : base(funcName)
            {
            }
        }
    }
}