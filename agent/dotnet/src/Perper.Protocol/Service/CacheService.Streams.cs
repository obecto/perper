using System;
using System.Collections;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Perper.Protocol.Cache.Instance;
using Perper.Protocol.Extensions;

namespace Perper.Protocol.Service
{
    public partial class CacheService
    {
        public Task StreamCreate<TParams>(string stream, string agent, string instance, string @delegate, StreamDelegateType delegateType, TParams parameters, bool ephemeral = true)
        {
            var streamData = StreamData.Create<TParams>(igniteBinary, agent, instance, @delegate, delegateType, ephemeral, parameters).Build();

            return streamsCache.PutIfAbsentOrThrowAsync(stream, streamData);
        }

        public async Task<IBinaryObject> StreamAddListener(string stream, string callerAgent, string caller, int parameter, Hashtable? filter = null, bool replay = false, bool localToData = false)
        {
            var streamListener = igniteBinary.ToBinary<IBinaryObject>(new StreamListener(callerAgent, caller, parameter, replay, localToData, filter));

            await streamsCache.OptimisticUpdateAsync(stream, value => StreamData.AddListener(value.ToBuilder(), streamListener).Build());

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

        public async Task<long> StreamWriteItem<TItem>(string stream, TItem item)
        {
            var itemsCache = ignite.GetCache<long, TItem>(stream);
            var key = GetCurrentTicks();

            await itemsCache.PutIfAbsentOrThrowAsync(key, item);

            return key;
        }

        public Task<TItem> StreamReadItem<TItem>(string cache, long key)
        {
            var itemsCache = ignite.GetCache<long, TItem>(cache);

            return itemsCache.GetAsync(key);
        }
    }
}