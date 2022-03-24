using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Linq;

namespace Perper.Extensions
{
    public class PerperCollection<T> : IAsyncEnumerable<T>
    {
        private readonly string name;

        public PerperCollection(string name)
        {
            this.name = name;
            var configCache = AsyncLocals.FabricService.GetCollectionCache<int>(AsyncLocals.Instance, $"{this.name}-meta");

            configCache.Put("start_index", 0);
            configCache.Put("end_index", -1);
            configCache.Put("count", 0);
        }

        public T this[string key]
        {
            get
            {
                var cacheEntry = AsyncLocals.FabricService
                         .GetCollectionCache<T>(AsyncLocals.Instance, name)
                         .AsCacheQueryable()
                         .FirstOrDefault(x => x.Key.EndsWith($"-{key}"));

                if (cacheEntry == null)
                {
                    return default!;
                }

                return cacheEntry.Value;
            }
            set
            {
                key = $"-{key}";

                var collectionCache = AsyncLocals.FabricService
                 .GetCollectionCache<T>(AsyncLocals.Instance, name);

                var cacheEntry = collectionCache
                 .AsCacheQueryable()
                 .FirstOrDefault(x => x.Key.EndsWith(key));

                if (cacheEntry == null)
                {
                    var configCache = AsyncLocals.FabricService.GetCollectionCache<int>(AsyncLocals.Instance, $"{name}-meta");
                    var lastIndex = configCache.Get("end_index") + 1;
                    configCache.Put("end_index", lastIndex);

                    key = $"{lastIndex}{key}";

                    collectionCache.Put(key, value);
                    configCache.Put("count", configCache.Get("count") + 1);
                }
                else
                {
                    collectionCache.Put(cacheEntry.Key, value);
                }
            }
        }
        public T this[int index]
        {
            get
            {
                var cacheEntry = AsyncLocals.FabricService
                    .GetCollectionCache<T>(AsyncLocals.Instance, name)
                    .AsCacheQueryable()
                    .FirstOrDefault(x => x.Key.StartsWith($"{index}-"));

                if (cacheEntry == null)
                {
                    return default!;
                }

                return cacheEntry.Value;
            }
            set
            {
                var key = $"{index}-";

                var collectionCache = AsyncLocals.FabricService
                 .GetCollectionCache<T>(AsyncLocals.Instance, name);

                var cacheEntry = collectionCache
                 .AsCacheQueryable()
                 .FirstOrDefault(x => x.Key.StartsWith(key));

                if (cacheEntry == null)
                {
                    throw new InvalidOperationException("Index out of range");
                }

                collectionCache.Put(cacheEntry.Key, value);
            }
        }

        public async Task<int> CountAsync() => await AsyncLocals.FabricService.GetCollectionCache<int>(AsyncLocals.Instance, $"{name}-meta").GetAsync("count").ConfigureAwait(false);

        public async Task AddAsync(string key, T value)
        {
            key = $"-{key}";

            var collectionCache = AsyncLocals.FabricService
             .GetCollectionCache<T>(AsyncLocals.Instance, name);

            var cacheEntry = collectionCache
             .AsCacheQueryable()
             .FirstOrDefault(x => x.Key.EndsWith(key));

            if(cacheEntry != null)
            {
                throw new ArgumentException("Key existing");
            }

            var configCache = AsyncLocals.FabricService.GetCollectionCache<int>(AsyncLocals.Instance, $"{name}-meta");
            var lastIndex = await GetNextIndex(configCache).ConfigureAwait(false);

            key = $"{lastIndex}{key}";
            await collectionCache.PutAsync(key, value).ConfigureAwait(false);
            await IncrementCount(configCache).ConfigureAwait(false);
        }


        public async Task AddAsync(T item)
        {
            var configCache = AsyncLocals.FabricService.GetCollectionCache<int>(AsyncLocals.Instance, $"{name}-meta");
            var key = await GetNextIndex(configCache).ConfigureAwait(false);

            var collectionCache = AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name);
            await collectionCache.PutAsync($"{key}", item).ConfigureAwait(false);
            await IncrementCount(configCache).ConfigureAwait(false);
        }

        public async Task ClearAsync()
        {
            var configCache = AsyncLocals.FabricService.GetCollectionCache<int>(AsyncLocals.Instance, $"{name}-meta");

            await configCache.PutAsync("start_index", 0).ConfigureAwait(false);
            await configCache.PutAsync("end_index", -1).ConfigureAwait(false);
            await configCache.PutAsync("count", 0).ConfigureAwait(false);

            await AsyncLocals.FabricService.GetCollectionCache<T>(AsyncLocals.Instance, name).ClearAsync().ConfigureAwait(false);
        }

        public async Task<bool> ContainsKeyAsync(string key) => throw new NotImplementedException();
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public async Task<int> IndexOfAsync(T item) => throw new NotImplementedException();
        public async Task InsertAsync(int index, T item) => throw new NotImplementedException();
        public async Task<bool> RemoveAsync(string key) => throw new NotImplementedException();
        public async Task RemoveAtAsync(int index) => throw new NotImplementedException();
        public async Task<bool> TryGetValueAsync(string key, [MaybeNullWhen(false)] out object value) => throw new NotImplementedException();

        public async Task<T> DequeueAsync() => throw new NotImplementedException();

        public async Task<T> PopAsync() => throw new NotImplementedException();

        public async Task<bool> SetIfNotExisting(string key, T value) => throw new NotImplementedException();

        public async Task<bool> SetIfNotChanged(string key, T value) => throw new NotImplementedException();

        private async Task IncrementCount(ICacheClient<string, int> configCache)
        {
            var count = await configCache.GetAsync("count").ConfigureAwait(false);
            await configCache.PutAsync("count", count + 1).ConfigureAwait(false);
        }

        private async Task<int> GetNextIndex(ICacheClient<string, int> configCache)
        {
            var lastIndex = await configCache.GetAsync("end_index").ConfigureAwait(false) + 1;
            await configCache.PutAsync("end_index", lastIndex).ConfigureAwait(false);

            return lastIndex;
        }
    }
}
