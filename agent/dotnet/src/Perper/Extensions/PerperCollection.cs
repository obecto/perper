using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Linq;

namespace Perper.Extensions
{
    public class PerperCollection<T> : IAsyncEnumerable<KeyValuePair<string, T>>
    {
        private readonly string name;

        public PerperCollection(string name)
        {
            this.name = name;
            var configCache = GetConfigCache(name);

            configCache.Put("start_index", 0);
            configCache.Put("end_index", -1);
            configCache.Put("count", 0);
        }

        public async Task<int> CountAsync() =>
            await GetConfigCache(name)
            .GetAsync("count").ConfigureAwait(false);

        public async Task AddAsync(string key, T value)
        {
            if (await ContainsKeyAsync(key).ConfigureAwait(false))
            {
                throw new ArgumentException("Key existing");
            }

            var collection = AsyncLocals.FabricService
                .GetCollectionCache<T>(AsyncLocals.Instance, name);

            var configCache = GetConfigCache(name);
            var lastIndex = await GetNextIndex(configCache).ConfigureAwait(false);

            key = $"{lastIndex}-{key}";
            await collection.PutAsync(key, value).ConfigureAwait(false);
            await IncrementCount(configCache).ConfigureAwait(false);
        }


        public async Task AddAsync(T item)
        {
            var configCache = GetConfigCache(name);
            var key = await GetNextIndex(configCache).ConfigureAwait(false);

            var collectionCache = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);
            await collectionCache.PutAsync($"{key}-", item).ConfigureAwait(false);
            await IncrementCount(configCache).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
            var configCache = GetConfigCache(name);

            await configCache.PutAsync("start_index", 0).ConfigureAwait(false);
            await configCache.PutAsync("end_index", -1).ConfigureAwait(false);
            await configCache.PutAsync("count", 0).ConfigureAwait(false);

            await AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name)
                .ClearAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> ContainsKeyAsync(string key) =>
            await AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name)
                .AsCacheQueryable()
                .ToAsyncEnumerable()
                .AnyAsync(x => x.Key.EndsWith($"-{key}", StringComparison.InvariantCulture))
                .ConfigureAwait(false);

        public async Task<int> IndexOfAsync(T item)
        {
            var collection = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);

            var result = await FirstOrDefaultAsync(collection,
                x => x.Value!.Equals(item))
                .ConfigureAwait(false);

            if (result == null)
            {
                return -1;
            }

            return int.Parse(result.Key.Split('-')[0], CultureInfo.InvariantCulture);
        }

        public async Task<bool> RemoveAsync(string key)
        {
            var collection = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);

            var result = await FindByKeyAsync(collection, key)
             .ConfigureAwait(false);

            if (result == null)
            {
                return false;
            }

            await DecrementCount(GetConfigCache(name))
                .ConfigureAwait(false);

            return await collection
                .RemoveAsync(result.Key)
                .ConfigureAwait(false);
        }


        public async Task RemoveAtAsync(int index)
        {
            var collection = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);

            var result = await FindByIndexAsync(collection, index)
                .ConfigureAwait(false);

            if (result == null)
            {
                return;
            }

            await DecrementCount(GetConfigCache(name))
                .ConfigureAwait(false);

            await collection
                .RemoveAsync(result.Key)
                .ConfigureAwait(false);
        }

        public async Task<(bool, T)> TryGetAsync(string key)
        {
            var collection = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);

            var result = await FindByKeyAsync(collection, key)
             .ConfigureAwait(false);

            if (result == null)
            {
                return (false, default(T)!);
            }

            return (true, result.Value);
        }

        public async Task<T> DequeueAsync()
        {
            var configCache = GetConfigCache(name);
            var lastIndex = await configCache.GetAsync("end_index").ConfigureAwait(false);
            var startIndex = await configCache.GetAsync("start_index").ConfigureAwait(false);

            if (startIndex > lastIndex)
            {
                throw new InvalidOperationException("The collection is empty!");
            }

            await configCache.PutAsync("start_index", startIndex + 1).ConfigureAwait(false);

            var collection = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);
            var result = await FindByIndexAsync(collection, startIndex)
                .ConfigureAwait(false);

            await collection.RemoveAsync(result.Key).ConfigureAwait(false);

            return result.Value;
        }

        public async Task<T> PopAsync()
        {
            var configCache = GetConfigCache(name);
            var lastIndex = await configCache.GetAsync("end_index").ConfigureAwait(false);
            var startIndex = await configCache.GetAsync("start_index").ConfigureAwait(false);

            if (startIndex > lastIndex)
            {
                throw new InvalidOperationException("The collection is empty!");
            }

            await configCache.PutAsync("end_index", lastIndex - 1).ConfigureAwait(false);

            var collection = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);
            var result = await FindByIndexAsync(collection, lastIndex)
                .ConfigureAwait(false);

            await collection.RemoveAsync(result.Key).ConfigureAwait(false);

            return result.Value;
        }

        public async Task<bool> SetIfNotExisting(string key, T value)
        {
            if (await ContainsKeyAsync(key)
                .ConfigureAwait(false))
            {
                return false;
            }

            await AddAsync(value)
                .ConfigureAwait(false);

            return true;
        }

        public async Task<bool> SetIfNotChanged(string key, T oldValue, T newValue)
        {

            var collection = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);
            var result = await FindByKeyAsync(collection, key)
                .ConfigureAwait(false);

            if (result == null || result.Value!.Equals(oldValue))
            {
                return false;
            }

            return await collection
                .ReplaceAsync(result.Key, newValue)
                .ConfigureAwait(false);
        }

        public Task<T> GetOrDefaultAsync(string key, T @default = default!) =>
            GetOrNewAsync(key, () => @default);

        public async Task<T> GetOrNewAsync(string key, Func<T> createFunc)
        {
            var (success, value) = await TryGetAsync(key).ConfigureAwait(false);

            return success ? value : createFunc();
        }

        public async IAsyncEnumerator<KeyValuePair<string, T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (var kv in AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name)
              .AsCacheQueryable()
              .OrderBy(x => int.Parse(x.Key.Split("-", StringSplitOptions.None)[0], CultureInfo.InvariantCulture))
              .Select(x => new KeyValuePair<string, T>(x.Key, x.Value))
              .ToAsyncEnumerable()
              .ConfigureAwait(false))
            {
                yield return kv;
            }
        }

        private static async Task IncrementCount(ICacheClient<string, int> configCache)
        {
            var count = await configCache.GetAsync("count").ConfigureAwait(false);

            await configCache.PutAsync("count", count + 1).ConfigureAwait(false);
        }

        private static async Task DecrementCount(ICacheClient<string, int> configCache)
        {
            var count = await configCache.GetAsync("count").ConfigureAwait(false);

            if (count > 0)
            {
                await configCache.PutAsync("count", count - 1).ConfigureAwait(false);
            }
        }

        private static async Task<int> GetNextIndex(ICacheClient<string, int> configCache)
        {
            var lastIndex = await configCache.GetAsync("end_index").ConfigureAwait(false) + 1;
            await configCache.PutAsync("end_index", lastIndex).ConfigureAwait(false);

            return lastIndex;
        }

        private static async Task<ICacheEntry<string, T>> FindByIndexAsync(ICacheClient<string, T> collection, int index) =>
            await FirstOrDefaultAsync(collection,
                x => x.Key.StartsWith($"{index}-", StringComparison.InvariantCulture))
                .ConfigureAwait(false);

        private static async Task<ICacheEntry<string, T>> FindByKeyAsync(ICacheClient<string, T> collection, string key) =>
            await FirstOrDefaultAsync(collection,
                x => x.Key.EndsWith($"-{key}", StringComparison.InvariantCulture))
                .ConfigureAwait(false);


        private static async Task<ICacheEntry<string, T>> FirstOrDefaultAsync(ICacheClient<string, T> collection, Func<ICacheEntry<string, T>, bool> predicate) =>
            await collection
                .AsCacheQueryable()
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(predicate)
                .ConfigureAwait(false);

        private static ICacheClient<string, int> GetConfigCache(string name) =>
            AsyncLocals.FabricService.GetCollectionCache<int>(AsyncLocals.Instance, $"{name}-meta");
    }
}
