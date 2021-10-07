using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Linq;

using Perper.Protocol.Instance;

namespace Perper.Protocol
{
    public partial class CacheService
    {
        public Task StreamCreate(string stream, string agent, string instance, string @delegate, StreamDelegateType delegateType, object[] parameters, bool ephemeral = true, string? indexType = null, Hashtable? indexFields = null)
        {
            var streamData = new StreamData(agent, instance, @delegate, delegateType, parameters, ephemeral, indexType, indexFields);

            return streamsCache.PutIfAbsentOrThrowAsync(stream, streamData);
        }

        public async Task StreamAddListener(string stream, string callerAgent, string callerInstance, string caller, int parameter, Hashtable? filter = null, bool replay = false, bool localToData = false)
        {
            var streamListener = new StreamListener(callerAgent, callerInstance, caller, parameter, replay, localToData, filter);

            await streamsCache.OptimisticUpdateAsync(stream, igniteBinary, value => { value.Listeners.Add(streamListener); }).ConfigureAwait(false);
        }

        public Task StreamRemoveListener(string stream, string caller, int parameter)
        {
            return streamsCache.OptimisticUpdateAsync(stream, igniteBinary, value =>
            {
                for (var i = 0 ; i < value.Listeners.Count ; i++)
                {
                    var listenerObject = value.Listeners[i];
                    var listener = listenerObject is IBinaryObject binObj ? binObj.Deserialize<StreamListener>() : (StreamListener)listenerObject;
                    if (listener.Caller == caller && listener.Parameter == parameter)
                    {
                        value.Listeners.RemoveAt(i);
                        break;
                    }
                }
            });
        }

        public async Task<long> StreamWriteItem<TItem>(string stream, TItem item, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream);
            if (keepBinary)
            {
                itemsCache = itemsCache.WithKeepBinary<long, TItem>();
            }

            var key = CurrentTicks;

            await itemsCache.PutIfAbsentOrThrowAsync(key, item).ConfigureAwait(false);

            return key;
        }

        public Task<TItem> StreamReadItem<TItem>(string cache, long key, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(cache).WithKeepBinary(keepBinary);

            return itemsCache.GetAsync(key);
        }

        public IQueryable<TItem> StreamGetQueryable<TItem>(string stream, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream).WithKeepBinary(keepBinary); // NOTE: Will not work with forwarding

            return itemsCache.AsCacheQueryable().Select(pair => pair.Value);
        }

        public async IAsyncEnumerable<TItem> StreamQuerySql<TItem>(string stream, string sql, object[] sqlParameters, bool keepBinary = false)
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

        public async Task<object[]> GetStreamParameters(string stream)
        {
            return (await streamsCache.GetAsync(stream).ConfigureAwait(false)).Parameters;
        }
    }
}