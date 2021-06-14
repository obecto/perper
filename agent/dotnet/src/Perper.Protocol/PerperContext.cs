using System;
using System.Collections;
using System.Threading.Tasks;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Perper.Protocol.Cache.Instance;

namespace Perper.Protocol
{
    public class PerperContext
    {
        public PerperContext(
            IIgniteClient ignite,
            string agent)
        {
            this.ignite = ignite;
            this.agent = agent;
            igniteBinary = ignite.GetBinary();
            streamsCache = ignite.GetCache<string, object>("streams").WithKeepBinary<string, IBinaryObject>();
            callsCache = ignite.GetCache<string, object>("calls").WithKeepBinary<string, IBinaryObject>();
        }

        private IIgniteClient ignite;
        private string agent;
        private IBinary igniteBinary;
        private ICacheClient<string, IBinaryObject> streamsCache;
        private ICacheClient<string, IBinaryObject> callsCache;

        private long GetCurrentTicks() => DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;

        public static async Task OptimisticUpdateAsync<K, V>(ICacheClient<K, V> cache, K key, Func<V, V> updateFunc)
        {
            while (true)
            {
                var existingValue = await cache.GetAsync(key);
                var newValue = updateFunc(existingValue);
                if (await cache.ReplaceAsync(key, existingValue, newValue))
                {
                    break;
                }
            }
        }

        public static async Task PutIfAbsentOrThrowAsync<K, V>(ICacheClient<K, V> cache, K key, V value)
        {
            var result = await cache.PutIfAbsentAsync(key, value);
            if (!result)
            {
                throw new Exception($"Duplicate cache item key! (key is {key})");
            }
        }

        public Task StreamCreate<TParams>(string stream, string instance, string @delegate, StreamDelegateType delegateType, TParams parameters, bool ephemeral = true)
        {
            var streamData = StreamData.Create<TParams>(igniteBinary, agent, instance, @delegate, delegateType, ephemeral, parameters).Build();

            return PutIfAbsentOrThrowAsync(streamsCache, stream, streamData);
        }

        public async Task<IBinaryObject> StreamAddListener(string stream, string caller, int parameter, Hashtable? filter = null, bool replay = false, bool localToData = false)
        {
            var streamListener = igniteBinary.ToBinary<IBinaryObject>(new StreamListener(agent, caller, parameter, replay, localToData, filter));

            await OptimisticUpdateAsync(streamsCache, stream, value => StreamData.AddListener(value.ToBuilder(), streamListener).Build());

            return streamListener;
        }

        public Task StreamRemoveListener(string stream, IBinaryObject streamListener)
        {
            return OptimisticUpdateAsync(streamsCache, stream, value => StreamData.RemoveListener(value.ToBuilder(), streamListener).Build());
        }

        public Task StreamRemoveListener(string stream, string caller, int parameter)
        {
            return OptimisticUpdateAsync(streamsCache, stream, value => StreamData.RemoveListener(value.ToBuilder(), caller, parameter).Build());
        }

        public async Task<long> StreamWriteItem<TItem>(string stream, TItem item)
        {
            var itemsCache = ignite.GetCache<long, TItem>(stream);
            var key = GetCurrentTicks();

            await PutIfAbsentOrThrowAsync(itemsCache, key, item);

            return key;
        }

        public Task<TItem> StreamReadItem<TItem>(string cache, long key)
        {
            var itemsCache = ignite.GetCache<long, TItem>(cache);

            return itemsCache.GetAsync(key);
        }

        public Task CallCreate<TParams>(string call, string instance, string @delegate, string callerAgent, string caller, TParams parameters, bool localToData = false)
        {
            var callData = CallData.Create<TParams>(igniteBinary, agent, instance, @delegate, callerAgent, caller, localToData, parameters).Build();

            return PutIfAbsentOrThrowAsync(callsCache, call, callData);
        }

        public Task CallWriteResult<TResult>(string call, TResult result)
        {
            return OptimisticUpdateAsync(callsCache, call, value => CallData.SetResult<TResult>(value.ToBuilder(), result).Build());
        }

        public Task CallWriteError(string call, string error)
        {
            return OptimisticUpdateAsync(callsCache, call, value => CallData.SetError(value.ToBuilder(), error).Build());
        }

        public Task CallWriteFinished(string call)
        {
            return OptimisticUpdateAsync(callsCache, call, value => CallData.SetFinished(value.ToBuilder()).Build());
        }

        public async Task<(string?, TResult)> CallReadErrorAndResult<TResult>(string call) // NOTE: should return (string?, TResult?)
        {
            var callData = await callsCache.GetAsync(call);

            var error = callData.HasField("error") ? callData.GetField<string>("error") : null;
            var result = callData.HasField("result") ? callData.GetField<TResult>("result") : default!;

            return (error, result);
        }
    }
}