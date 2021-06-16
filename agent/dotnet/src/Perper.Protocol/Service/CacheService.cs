using System;
using System.Collections;
using System.Threading.Tasks;
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
            this.ignite = ignite;
            igniteBinary = ignite.GetBinary();
            streamsCache = ignite.GetCache<string, object>("streams").WithKeepBinary<string, IBinaryObject>();
            callsCache = ignite.GetCache<string, object>("calls").WithKeepBinary<string, IBinaryObject>();
        }

        private IIgniteClient ignite;
        private IBinary igniteBinary;
        private ICacheClient<string, IBinaryObject> streamsCache;
        private ICacheClient<string, IBinaryObject> callsCache;

        public long GetCurrentTicks() => DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;

        public string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";
    }
}