using System;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public partial class CacheService
    {
        public CacheService(IIgniteClient ignite)
        {
            Ignite = ignite;
            igniteBinary = ignite.GetBinary();
            executionsCache = ignite.GetOrCreateCache<string, ExecutionData>("executions");
            streamListenersCache = ignite.GetOrCreateCache<string, StreamListener>("stream-listeners");
            instancesCache = ignite.GetOrCreateCache<string, InstanceData>("instances");
        }

        public IIgniteClient Ignite { get; }
        private readonly IBinary igniteBinary;
        private readonly ICacheClient<string, ExecutionData> executionsCache;
        private readonly ICacheClient<string, StreamListener> streamListenersCache;
        private readonly ICacheClient<string, InstanceData> instancesCache;

        public static long CurrentTicks => DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;

        public static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";
    }
}