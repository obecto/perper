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
            await _output.AddAsync(CreateBinaryObject(functionName, parameters));
            return new PerperStreamHandle(functionName, typeof(T));
        }

        private IBinaryObject CreateBinaryObject(string cacheName, object parameters, Type cacheType = default)
        {
            var cacheObjectBuilder = cacheType == default
                ? _binary.GetCacheObjectBuilder(cacheName)
                : _binary.GetCacheObjectBuilder(cacheName, cacheType);

            var properties = parameters.GetType().GetProperties();
            foreach (var propertyInfo in properties)
            {
                var propertyValue = propertyInfo.GetValue(properties);
                if (propertyValue is PerperStreamHandle streamHandle)
                {
                    cacheObjectBuilder.SetField(propertyInfo.Name,
                        _binary.GetCacheObjectBuilder(streamHandle.CacheName, streamHandle.CacheType).Build());
                }
                else
                {
                    cacheObjectBuilder.SetField(propertyInfo.Name, propertyValue);
                }
            }

            return cacheObjectBuilder.Build();
        }
    }
}