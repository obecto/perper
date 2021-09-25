using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Linq;

using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Extensions;

namespace Perper.Protocol.Service
{
    public partial class CacheService
    {
        public Task StreamCreate(string stream, string agent, string instance, string @delegate, StreamDelegateType delegateType, object[] parameters, bool ephemeral = true, string? indexType = null, Hashtable? indexFields = null)
        {
            var streamData = StreamData.Create(igniteBinary, agent, instance, @delegate, delegateType, ephemeral, parameters, indexType, indexFields).Build();

            return streamsCache.PutIfAbsentOrThrowAsync(stream, streamData);
        }

        public async Task<IBinaryObject> StreamAddListener(string stream, string callerAgent, string callerInstance, string caller, int parameter, Hashtable? filter = null, bool replay = false, bool localToData = false)
        {
            var streamListener = igniteBinary.ToBinary<IBinaryObject>(new StreamListener(callerAgent, callerInstance, caller, parameter, replay, localToData, filter));

            await streamsCache.OptimisticUpdateAsync(stream, value => StreamData.AddListener(value.ToBuilder(), streamListener).Build()).ConfigureAwait(false);

            return streamListener;
        }

        public Task StreamRemoveListener(string stream, IBinaryObject streamListener)
        {
            return streamsCache.OptimisticUpdateAsync(stream, value => StreamData.RemoveListener(value.ToBuilder(), streamListener).Build());
        }

        public Task StreamRemoveListener(string stream, string caller, int parameter)
        {
            return streamsCache.OptimisticUpdateAsync(stream, value => StreamData.RemoveListener(value.ToBuilder(), caller, parameter).Build());
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
            var itemsCache = Ignite.GetCache<long, TItem>(cache);
            if (keepBinary)
            {
                itemsCache = itemsCache.WithKeepBinary<long, TItem>();
            }

            return itemsCache.GetAsync(key);
        }

        public IQueryable<TItem> StreamGetQueryable<TItem>(string stream, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream); // NOTE: Will not work with forwarding
            if (keepBinary)
            {
                itemsCache = itemsCache.WithKeepBinary<long, TItem>();
            }

            return itemsCache.AsCacheQueryable().Select(pair => pair.Value);
        }

        public async IAsyncEnumerable<TItem> StreamQuerySql<TItem>(string stream, string sql, object[] sqlParameters, bool keepBinary = false)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream); // NOTE: Will not work with forwarding
            if (keepBinary)
            {
                itemsCache = itemsCache.WithKeepBinary<long, TItem>();
            }

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
            object[] parameters = default!;
            var streamData = await streamsCache.GetAsync(stream).ConfigureAwait(false);

            if (streamData.HasField("parameters"))
            {
                parameters = streamData.GetField<object[]>("parameters");
            }

            for (var i = 0 ; i < parameters.Length ; i++)
            {
                if (parameters[i] is IBinaryObject binaryObject)
                {
                    parameters[i] = binaryObject.Deserialize<object>();
                }
            }

            return parameters;
        }
    }
}