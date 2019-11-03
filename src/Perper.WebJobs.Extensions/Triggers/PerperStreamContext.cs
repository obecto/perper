using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Ignite.Extensions;
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

        public async Task CallStreamAction(string actionName, object parameters)
        {
            await _output.AddAsync(CreateBinaryObject(actionName, parameters));
        }

        public async Task<IPerperStreamHandle> CallStreamFunction<T>(string functionName, object parameters)
        {
            await _output.AddAsync(CreateBinaryObject(functionName, parameters, typeof(T)));
            return new PerperStreamHandle(functionName, typeof(T));
        }

        private IBinaryObject CreateBinaryObject(string cacheName, object parameters)
        {
            return CreateBinaryObject(_binary.GetCacheObjectBuilder(cacheName), parameters);
        }

        private IBinaryObject CreateBinaryObject(string cacheName, object parameters, Type cacheType)
        {
            return CreateBinaryObject(_binary.GetCacheObjectBuilder(cacheName, cacheType), parameters);
        }

        private IBinaryObject CreateBinaryObject(IBinaryObjectBuilder builder, object parameters)
        {
            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(properties);
                if (propertyValue is PerperStreamHandle streamHandle)
                {
                    builder.SetField(propertyInfo.Name,
                        _binary.GetCacheObjectBuilder(streamHandle.CacheName, streamHandle.CacheType).Build());
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