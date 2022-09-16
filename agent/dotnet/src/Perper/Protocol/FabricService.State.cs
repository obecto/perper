using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Linq;

using Perper.Model;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperStates
    {
        #region Dictionaries
        (PerperDictionary Dictionary, Func<Task> Create) IPerperStates.CreateDictionary(PerperStateOptions? options)
        {
            var name = GenerateName("");
            return (new(name), () => CreateCache(name, options));
        }

        PerperDictionary IPerperStates.GetInstanceDictionary(PerperAgent instance) =>
            new($"{instance.Instance}-");

        async Task IPerperStates.SetAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue value)
        {
            if (value != null)
            {
                await GetItemsCache<TKey, TValue>(dictionary).PutAsync(key, value).ConfigureAwait(false);
            }
            else
            {
                await GetItemsCache<TKey, TValue>(dictionary).RemoveAsync(key).ConfigureAwait(false);
            }
        }

        async Task<bool> IPerperStates.SetIfNotChangedAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue oldValue, TValue value)
        {
            return await GetItemsCache<TKey, TValue>(dictionary).ReplaceAsync(key, oldValue, value).ConfigureAwait(false);
        }

        async Task<bool> IPerperStates.SetIfNotExistingAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue value)
        {
            return await GetItemsCache<TKey, TValue>(dictionary).PutIfAbsentAsync(key, value).ConfigureAwait(false);
        }

        async Task<(bool Exists, TValue Value)> IPerperStates.TryGetAsync<TKey, TValue>(PerperDictionary dictionary, TKey key)
        {
            var result = await GetItemsCache<TKey, TValue>(dictionary).TryGetAsync(key).ConfigureAwait(false);

            return (result.Success, result.Value);
        }

        async Task<(bool Exists, TValue Value)> IPerperStates.TryGetAndReplaceAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue newValue)
        {
            var result = await GetItemsCache<TKey, TValue>(dictionary).GetAndPutAsync(key, newValue).ConfigureAwait(false);

            return (result.Success, result.Value);
        }

        async Task<(bool Exists, TValue Value)> IPerperStates.TryGetAndRemoveAsync<TKey, TValue>(PerperDictionary dictionary, TKey key)
        {
            var result = await GetItemsCache<TKey, TValue>(dictionary).GetAndRemoveAsync(key).ConfigureAwait(false);

            return (result.Success, result.Value);
        }

        async Task<bool> IPerperStates.RemoveAsync<TKey>(PerperDictionary dictionary, TKey key)
        {
            return await GetItemsCache<TKey, object>(dictionary).RemoveAsync(key).ConfigureAwait(false);
        }

        async Task<bool> IPerperStates.RemoveIfNotChangedAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue oldValue)
        {
            return await GetItemsCache<TKey, TValue>(dictionary).RemoveAsync(key, oldValue).ConfigureAwait(false);
        }

        async Task<bool> IPerperStates.ContainsKeyAsync<TKey>(PerperDictionary dictionary, TKey key)
        {
            return await GetItemsCache<TKey, object>(dictionary).ContainsKeyAsync(key).ConfigureAwait(false);
        }

        async IAsyncEnumerable<(TKey Key, TValue Value)> IPerperStates.EnumerateItemsAsync<TKey, TValue>(PerperDictionary dictionary)
        {
            using var cursor = GetItemsCache<TKey, TValue>(dictionary).Query(new ScanQuery<TKey, TValue>());
            await foreach (var item in cursor.ToAsyncEnumerable())
            {
                yield return (item.Key, item.Value);
            }
        }

        Task<int> IPerperStates.CountAsync(PerperDictionary dictionary)
        {
            return Task.Run(() => GetItemsCache<object, object>(dictionary).AsCacheQueryable().Count());
        }

        async Task IPerperStates.ClearAsync(PerperDictionary dictionary)
        {
            await GetItemsCache<object, object>(dictionary).RemoveAllAsync().ConfigureAwait(false);
        }

        Task IPerperStates.DestroyAsync(PerperDictionary dictionary)
        {
            return Task.Run(() =>
                Ignite.DestroyCache(GetItemsCache<object, object>(dictionary).Name));
        }

        public ICacheClient<TK, TV> GetItemsCache<TK, TV>(PerperDictionary dictionary)
        {
            return GetStateCache<TK, TV>(dictionary.Name);
        }
        #endregion Dictionaries

        #region Lists
        public const string StartIndexKey = "start_index";
        public const string EndIndexKey = "end_index";

        (PerperList List, Func<Task> Create) IPerperStates.CreateList(PerperStateOptions? options)
        {
            var name = GenerateName("");
            return (new(name), () => CreateCache(name, options));
        }

        PerperList IPerperStates.GetInstanceChildrenList(PerperAgent instance) =>
            new($"{instance.Instance}-children");

        async IAsyncEnumerable<TValue> IPerperStates.EnumerateAsync<TValue>(PerperList list)
        {
            var configurationCache = GetConfigurationCache(list);
            var itemsCache = GetItemsCache<TValue>(list);

            var index = await configurationCache.GetOrDefaultAsync(StartIndexKey, 0).ConfigureAwait(false);
            while (true)
            {
                var item = await itemsCache.TryGetAsync(index).ConfigureAwait(false);
                if (item.Success)
                {
                    yield return item.Value;
                }
                else if (index >= await configurationCache.GetOrDefaultAsync(EndIndexKey, 0).ConfigureAwait(false))
                {
                    break;
                }
                else
                {
                    // we are missing an item and are still before the end. Probably raced, try next item.
                }
                index++;
            }
        }

        async Task<int> IPerperStates.CountAsync(PerperList list)
        {
            var configurationCache = GetConfigurationCache(list);

            return await configurationCache.GetOrDefaultAsync(EndIndexKey, 0).ConfigureAwait(false) - await configurationCache.GetOrDefaultAsync(StartIndexKey, 0).ConfigureAwait(false);
        }

        async Task IPerperStates.AddAsync<TValue>(PerperList list, TValue value)
        {
            var index = await GetNextIndexAsync(GetConfigurationCache(list), EndIndexKey, 1).ConfigureAwait(false);

            await GetItemsCache<TValue>(list).PutIfAbsentOrThrowAsync(index, value).ConfigureAwait(false);
        }

        async Task<TValue> IPerperStates.PopAsync<TValue>(PerperList list)
        {
            while (true)
            {
                var index = await GetNextIndexAsync(GetConfigurationCache(list), EndIndexKey, -1).ConfigureAwait(false);

                var result = await GetItemsCache<TValue>(list).GetAndRemoveAsync(index).ConfigureAwait(false);
                if (result.Success)
                {
                    return result.Value;
                }
            }
        }

        async Task<TValue> IPerperStates.DequeueAsync<TValue>(PerperList list)
        {
            while (true)
            {
                var index = await GetNextIndexAsync(GetConfigurationCache(list), StartIndexKey, 1).ConfigureAwait(false);

                var result = await GetItemsCache<TValue>(list).GetAndRemoveAsync(index).ConfigureAwait(false);
                if (result.Success)
                {
                    return result.Value;
                }
            }
        }

        async Task IPerperStates.InsertAtAsync<TValue>(PerperList list, int atIndex, TValue value)
        {
            var itemIndex = await GetConfigurationCache(list).GetOrDefaultAsync(StartIndexKey, 0).ConfigureAwait(false) + atIndex;
            await InsertAtRawAsync(list, itemIndex, value).ConfigureAwait(false);
        }

        public async Task InsertAtRawAsync<TValue>(PerperList list, int itemIndex, TValue value)
        {
            var configurationCache = GetConfigurationCache(list);
            var itemsCache = GetItemsCache<TValue>(list);

            var index = itemIndex;
            while (true)
            {
                var result = await itemsCache.GetAndReplaceAsync(index, value).ConfigureAwait(false);
                if (result.Success)
                {
                    value = result.Value;
                }
                else if (index >= await configurationCache.GetOrDefaultAsync(EndIndexKey, 0).ConfigureAwait(false))
                {
                    break;
                }
                else
                {
                    // we are missing an item and are still before the end. Probably raced, try next item.
                }
                index++;
            }

            await ((IPerperStates)this).AddAsync(list, value).ConfigureAwait(false);
        }

        async Task IPerperStates.RemoveAtAsync(PerperList list, int atIndex)
        {
            var itemIndex = await GetConfigurationCache(list).GetOrDefaultAsync(StartIndexKey, 0).ConfigureAwait(false) + atIndex;
            await RemoveAtRawAsync(list, itemIndex).ConfigureAwait(false);
        }

        public async Task RemoveAtRawAsync(PerperList list, int itemIndex)
        {
            var configurationCache = GetConfigurationCache(list);
            var itemsCache = GetItemsCache<object>(list);

            var index = await configurationCache.GetOrDefaultAsync(EndIndexKey, 0).ConfigureAwait(false) - 1;

            if (itemIndex > index)
            {
                return;
            }

            var value = await itemsCache.GetAsync(index).ConfigureAwait(false);

            for (; index <= itemIndex ; index--)
            {
                var result = await itemsCache.GetAndReplaceAsync(index, value).ConfigureAwait(false);
                if (result.Success)
                {
                    value = result.Value;
                }
            }
        }

        async Task<TValue> IPerperStates.GetAtAsync<TValue>(PerperList list, int atIndex)
        {
            var itemIndex = await GetConfigurationCache(list).GetOrDefaultAsync(StartIndexKey, 0).ConfigureAwait(false) + atIndex;

            return await GetAtRawAsync<TValue>(list, itemIndex).ConfigureAwait(false);
        }

        public async Task<TValue> GetAtRawAsync<TValue>(PerperList list, int itemIndex)
        {
            return await GetItemsCache<TValue>(list).GetAsync(itemIndex).ConfigureAwait(false);
        }

        async Task IPerperStates.SetAtAsync<TValue>(PerperList list, int atIndex, TValue value)
        {
            var itemIndex = await GetConfigurationCache(list).GetOrDefaultAsync(StartIndexKey, 0).ConfigureAwait(false) + atIndex;

            await SetAtRawAsync(list, itemIndex, value).ConfigureAwait(false);
        }

        public async Task SetAtRawAsync<TValue>(PerperList list, int itemIndex, TValue value)
        {
            await GetItemsCache<TValue>(list).PutAsync(itemIndex, value).ConfigureAwait(false);
        }

        async Task<int> IPerperStates.IndexOfAsync<TValue>(PerperList list, TValue value)
        {
            var itemIndex = await IndexOfRawAsync(list, value).ConfigureAwait(false);
            return itemIndex == null ? -1 : itemIndex.Value - await GetConfigurationCache(list).GetOrDefaultAsync(StartIndexKey, 0).ConfigureAwait(false);
        }

        public async Task<int?> IndexOfRawAsync<TValue>(PerperList list, TValue value)
        {
            // TODO: Try-catch; fall back to something else if SQL cache queries are not configured.
            var result = await Task.Run(() => GetItemsCache<TValue>(list).AsCacheQueryable()
                .OrderBy(x => x.Key)
                .FirstOrDefault(x => x.Value!.Equals(value)))
                .ConfigureAwait(false);

            return result?.Key;
        }

        async Task<bool> IPerperStates.RemoveAsync<TValue>(PerperList list, TValue value)
        {
            var itemIndex = await IndexOfRawAsync(list, value).ConfigureAwait(false);
            if (itemIndex == null)
            {
                return false;
            }
            else
            {
                await RemoveAtRawAsync(list, itemIndex.Value).ConfigureAwait(false);
                return true;
            }
        }

        async Task IPerperStates.ClearAsync(PerperList list)
        {
            await GetItemsCache<object>(list).RemoveAllAsync().ConfigureAwait(false);
            await GetConfigurationCache(list).RemoveAllAsync().ConfigureAwait(false);
        }

        Task IPerperStates.DestroyAsync(PerperList list)
        {
            return Task.Run(() =>
            {
                Ignite.DestroyCache(GetItemsCache<object>(list).Name);
                Ignite.DestroyCache(GetConfigurationCache(list).Name);
            });
        }

        private static async Task<int> GetNextIndexAsync(ICacheClient<string, int> configurationCache, string key, int difference = 1)
        {
            while (true)
            {
                var existing = await configurationCache.TryGetAsync(key).ConfigureAwait(false);
                if (existing.Success)
                {
                    var newValue = existing.Value + difference;
                    if (await configurationCache.ReplaceAsync(key, existing.Value, newValue).ConfigureAwait(false))
                    {
                        return existing.Value;
                    }
                }
                else
                {
                    if (await configurationCache.PutIfAbsentAsync(key, difference).ConfigureAwait(false))
                    {
                        return 0;
                    }
                }

            }
        }

        public ICacheClient<int, TV> GetItemsCache<TV>(PerperList list)
        {
            return GetStateCache<int, TV>(list.Name);
        }

        public ICacheClient<string, int> GetConfigurationCache(PerperList list)
        {
            return Ignite.GetOrCreateCache<string, int>(list.Name);
        }
        #endregion Lists

        private Task CreateCache(string name, PerperStateOptions? options)
        {
            var cacheConfiguration = FabricCaster.GetCacheConfiguration(name, options);
            cacheConfiguration.Name = name;
            Ignite.GetOrCreateCache<object, object>(cacheConfiguration);
            return Task.CompletedTask;
        }

        public ICacheClient<TK, TV> GetStateCache<TK, TV>(string name)
        {
            var queryEntity = new QueryEntity(typeof(TK), typeof(TV));

            if (queryEntity.Fields == null)
            {
                return Ignite.GetOrCreateCache<TK, TV>(name).WithKeepBinary(FabricCaster.TypeShouldKeepBinary(typeof(TV)));
            }
            else
            {
                return Ignite.GetOrCreateCache<TK, TV>(new CacheClientConfiguration(name, queryEntity)).WithKeepBinary(FabricCaster.TypeShouldKeepBinary(typeof(TV)));
            }
        }
    }
}