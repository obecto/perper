using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;
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

        public async Task<IBinaryObject> StreamAddListener(string stream, string callerAgent, string caller, int parameter, Hashtable? filter = null, bool replay = false, bool localToData = false)
        {
            var streamListener = igniteBinary.ToBinary<IBinaryObject>(new StreamListener(callerAgent, caller, parameter, replay, localToData, filter));

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

        public async Task<long> StreamWriteItem<TItem>(string stream, TItem item)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream);
            var key = CurrentTicks;

            await itemsCache.PutIfAbsentOrThrowAsync(key, item).ConfigureAwait(false);

            return key;
        }

        public Task<TItem> StreamReadItem<TItem>(string cache, long key)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(cache);

            return itemsCache.GetAsync(key);
        }

        public IQueryable<TItem> StreamGetQueryable<TItem>(string stream)
        {
            var itemsCache = Ignite.GetCache<long, TItem>(stream); // NOTE: Will not work with forwarding

            return itemsCache.AsCacheQueryable().Select(pair => pair.Value);
        }

        public async Task<object[]> GetStreamParameters(string stream)
        {
            object[] parameters = default!;
            var streamData = await streamsCache.GetAsync(stream).ConfigureAwait(false);

            if (streamData.HasField("parameters"))
            {
                var field = streamData.GetField<object>("parameters");
                if (field is IBinaryObject binaryObject)
                {
                    parameters = binaryObject.Deserialize<object[]>();
                }
                else if (field is object[] tfield)
                {
                    parameters = tfield;
                }
                else
                {
                    throw new ArgumentException($"Can't convert result from {field?.GetType()?.ToString() ?? "Null"} to {typeof(object[])}");
                }
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