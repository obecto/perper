using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Linq;

namespace Perper.Extensions.Collections
{

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class PerperList<T> : IAsyncList<T>, IList<T>
    {
        private readonly string instance;
        private readonly string name;

        public PerperList(string instance, string name)
        {
            this.instance = instance;
            this.name = name;

            var configCache = ConfigCache;

            configCache.Put("start_index", 0);
            configCache.Put("end_index", 0);
        }

        public T this[int index] { get => DataCache.Get(ConfigCache["start_index"] + index); set => DataCache.Put(ConfigCache["start_index"] + index, value); }

        public int Count
        {
            get
            {
                var configCache = ConfigCache;

                return configCache["end_index"] - configCache["start_index"];
            }
        }

        public bool IsReadOnly => false;


        public string Instance => instance;
        public string Name => name;

        public void Add(T item) => DataCache.Put(GetNextIndex("end_index"), item);
        public async Task AddAsync(T item) =>
            await DataCache
            .PutAsync(await GetNextIndexAsync("end_index").ConfigureAwait(false), item)
            .ConfigureAwait(false);

        public void Clear()
        {
            var configCache = ConfigCache;

            DataCache.Clear();
            configCache.Put("end_index", configCache.Get("start_index"));
        }

        public async Task ClearAsync()
        {
            var configCache = ConfigCache;

            await DataCache.ClearAsync().ConfigureAwait(false);

            await configCache.PutAsync("end_index", await configCache
                .GetAsync("start_index")
                .ConfigureAwait(false))
                    .ConfigureAwait(false);
        }

        public bool Contains(T item) => DataCache.AsCacheQueryable().Any(x => x.Value!.Equals(item));

        public async Task<bool> ContainsAsync(T item) => await DataCache.AsCacheQueryable()
            .ToAsyncEnumerable()
            .AnyAsync(x => x.Value!.Equals(item))
            .ConfigureAwait(false);

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public async Task<int> CountAsync()
        {
            var configCache = ConfigCache;

            return await configCache.GetAsync("end_index").ConfigureAwait(false)
                - await configCache.GetAsync("start_index").ConfigureAwait(false);
        }

        public async Task<T> DequeueAsync()
        {
            var index = await GetNextIndexAsync("start_index").ConfigureAwait(false);
            return (await DataCache.GetAndRemoveAsync(index).ConfigureAwait(false)).Value;
        }

        public async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (var value in DataCache
                .AsCacheQueryable()
                .OrderBy(x => x.Key)
                .Select(x => x.Value)
                .ToAsyncEnumerable()
                .ConfigureAwait(false))
            {
                yield return value;
            }
        }

        public IEnumerator<T> GetEnumerator() => GetEnumerator();

        public int IndexOf(T item)
        {
            var result = DataCache.AsCacheQueryable().FirstOrDefault(x => x.Value!.Equals(item));

            if (result == null)
            {
                return -1;
            }

            return result.Key - ConfigCache["start_index"];
        }


        public async Task<int> IndexOfAsync(T item)
        {
            var result = await DataCache.AsCacheQueryable()
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(x => x.Value!.Equals(item))
                .ConfigureAwait(false);

            if (result == null)
            {
                return -1;
            }

            return result.Key - await ConfigCache.GetAsync("start_index").ConfigureAwait(false);
        }

        public void Insert(int index, T item)
        {
            var dataCache = DataCache;

            var startIndex = ConfigCache["start_index"];
            index += startIndex;
            var count = Count + startIndex;
            for (var i = index ; i < count ; i++)
            {
                item = dataCache.GetAndReplace(i, item).Value;
            }

            Add(item);
        }

        public async Task InsertAsync(int index, T item)
        {
            var dataCache = DataCache;

            var startIndex = await ConfigCache.GetAsync("start_index").ConfigureAwait(false);
            index += startIndex;
            var count = Count + startIndex;
            for (var i = index ; i < count ; i++)
            {
                item = (await dataCache.GetAndReplaceAsync(i, item).ConfigureAwait(false)).Value;
            }

            await AddAsync(item).ConfigureAwait(false);
        }

        public async Task<T> PopAsync() => await DataCache.GetAsync(
            await GetPrevIndexAsync("end_index").ConfigureAwait(false))
            .ConfigureAwait(false);

        public bool Remove(T item)
        {
            var dataCache = DataCache;

            var index = dataCache
                .AsCacheQueryable()
                .FirstOrDefault(x => x.Value!.Equals(item))?.Key;

            if (index == null)
            {
                return false;
            }

            RemoveAt(index.Value);

            return true;
        }

        public async Task<bool> RemoveAsync(T item)
        {
            var dataCache = DataCache;

            var index = (await dataCache
                .AsCacheQueryable()
                .ToAsyncEnumerable()
                .FirstOrDefaultAsync(x => x.Value!.Equals(item)).ConfigureAwait(false))?.Key;

            if (index == null)
            {
                return false;
            }

            await RemoveAtAsync(index.Value).ConfigureAwait(false);

            return true;
        }

        public void RemoveAt(int index)
        {
            var dataCache = DataCache;

            var startIndex = ConfigCache["start_index"];
            var idx = startIndex + Count - 1;

            var item = dataCache.Get(idx);

            for (var i = idx - 1 ; i >= index ; i--)
            {
                item = dataCache.GetAndReplace(i, item).Value;
            }
        }

        public async Task RemoveAtAsync(int index)
        {
            var dataCache = DataCache;

            var startIndex = await ConfigCache.GetAsync("start_index").ConfigureAwait(false);
            var idx = startIndex + Count - 1;

            var item = (await dataCache.GetAndRemoveAsync(idx).ConfigureAwait(false)).Value;

            for (var i = idx - 1 ; i >= index ; i--)
            {
                item = (await dataCache.GetAndReplaceAsync(i, item).ConfigureAwait(false)).Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var value in DataCache
                .AsCacheQueryable()
                .Select(x => x.Value))
            {
                yield return value;
            }
        }

        private async Task<int> GetNextIndexAsync(string index)
        {
            var configCache = ConfigCache;

            while (true)
            {
                var existingValue = await configCache.GetAsync(index).ConfigureAwait(false);
                var newValue = existingValue + 1;
                if (await configCache.ReplaceAsync(index, existingValue, newValue).ConfigureAwait(false))
                {
                    return existingValue;
                }
            }
        }

        private int GetNextIndex(string index)
        {
            var configCache = ConfigCache;

            while (true)
            {
                var existingValue = configCache.Get(index);
                var newValue = existingValue + 1;
                if (configCache.Replace(index, existingValue, newValue))
                {
                    return existingValue;
                }
            }
        }

        private async Task<int> GetPrevIndexAsync(string index)
        {
            var configCache = ConfigCache;

            while (true)
            {
                var existingValue = await configCache.GetAsync(index).ConfigureAwait(false);
                var newValue = existingValue - 1;
                if (await configCache.ReplaceAsync(index, existingValue, newValue).ConfigureAwait(false))
                {
                    return existingValue;
                }
            }
        }

        private ICacheClient<string, int> ConfigCache => AsyncLocals.FabricService.GetListMetaCache<int>(instance, name);

        private ICacheClient<int, T> DataCache => AsyncLocals.FabricService.GetListCache<T>(instance, name);

        public override string ToString() => $"PerperList({Instance},{Name})";
    }
}
