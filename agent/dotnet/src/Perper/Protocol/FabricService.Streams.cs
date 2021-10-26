using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Linq;

using Perper.Protocol.Cache;

namespace Perper.Protocol
{
    public partial class FabricService
    {
        public async Task CreateStream(string stream, params QueryEntity[] indexes)
        {
            await Task.Run(() => Ignite.CreateCache<long, object>(new CacheClientConfiguration(stream, indexes))).ConfigureAwait(false);
        }

        public async Task RemoveStream(string stream)
        {
            await Task.Run(() => Ignite.DestroyCache(stream)).ConfigureAwait(false);
        }

        public const long ListenerPersistAll = long.MinValue;
        public const long ListenerJustTrigger = long.MaxValue;

        public async Task SetStreamListenerPosition(string listener, string stream, long position)
        {
            await StreamListenersCache.PutAsync(listener, new StreamListener(stream, position)).ConfigureAwait(false);
        }

        public async Task RemoveStreamListener(string listener)
        {
            await StreamListenersCache.RemoveAsync(listener).ConfigureAwait(false);
        }

        public async Task WriteStreamItem<TItem>(string stream, long key, TItem item, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream);
            if (keepBinary)
            {
                itemsCache = itemsCache.WithKeepBinary<long, TItem>();
            }

            await itemsCache.PutIfAbsentOrThrowAsync(key, item).ConfigureAwait(false);
        }

        public async Task<TItem> ReadStreamItem<TItem>(string cache, long key, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(cache).WithKeepBinary(keepBinary);

            return await itemsCache.GetAsync(key).ConfigureAwait(false);
        }

        public IQueryable<TItem> QueryStream<TItem>(string stream, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream).WithKeepBinary(keepBinary); // NOTE: Will not work with forwarding

            return itemsCache.AsCacheQueryable().Select(pair => pair.Value);
        }

        public async IAsyncEnumerable<TItem> QueryStreamSql<TItem>(string stream, string sql, object[] sqlParameters, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream).WithKeepBinary(keepBinary); // NOTE: Will not work with forwarding

            var query = new SqlFieldsQuery(sql, false, sqlParameters);

            using var cursor = itemsCache.Query(query);
            using var enumerator = cursor.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext).ConfigureAwait(false)) // Blocking, should run in background
            {
                yield return (TItem)enumerator.Current[0];
            }
        }
    }
}