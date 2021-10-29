using System;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Perper.Protocol.Cache.Instance;

namespace Perper.Protocol.Service
{
    public partial class CacheService
    {
        public CacheService(IIgniteClient ignite)
        {
            Ignite = ignite;
            igniteBinary = ignite.GetBinary();
            streamsCache = ignite.GetCache<string, object>("streams").WithKeepBinary<string, IBinaryObject>();
            callsCache = ignite.GetCache<string, object>("calls").WithKeepBinary<string, IBinaryObject>();
            instancesCache = ignite.GetCache<string, InstanceData>("instances");
        }

        public IIgniteClient Ignite { get; }
        private readonly IBinary igniteBinary;
        private readonly ICacheClient<string, IBinaryObject> streamsCache;
        private readonly ICacheClient<string, IBinaryObject> callsCache;
        private readonly ICacheClient<string, InstanceData> instancesCache;

        public static long CurrentTicks => DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;

        public string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";
    }
}