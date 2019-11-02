using System;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Triggers
{
    public class PerperStreamHandle : IPerperStreamHandle
    {
        public Type CacheType { get; }
        public string CacheName { get; }
        
        public PerperStreamHandle(string cacheName, Type cacheType)
        {
            CacheName = cacheName;
            CacheType = cacheType;
        }
    }
}