using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Linq;

namespace Perper.Extensions.Collections
{
    [SuppressMessage("Style", "IDE0032:Use auto property", Justification = "We want camelCase field names for Ignite's reflection")]
    public class PerperDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IAsyncDictionary<TKey, TValue>
    {
        private readonly string instance;
        private readonly string name;

        public PerperDictionary(string instance, string name)
        {
            this.instance = instance;
            this.name = name;
        }

        public TValue this[TKey key] { get => DataCache[key]; set => DataCache[key] = value; }

        public ICollection<TKey> Keys => DataCache.AsCacheQueryable().Select(x => x.Key).ToList();

        public ICollection<TValue> Values => DataCache.AsCacheQueryable().Select(x => x.Value).ToList();

        public int Count => DataCache.AsCacheQueryable().Count();

        public bool IsReadOnly => true;

        public string Instance => instance;
        public string Name => name;

        public void Add(TKey key, TValue value)
        {
            if (!DataCache.PutIfAbsent(key, value))
            {
                throw new ArgumentException("An element with the same key already exists in the dictionary");
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (!DataCache.PutIfAbsent(item.Key, item.Value))
            {
                throw new ArgumentException("An element with the same key already exists in the dictionary");
            }
        }

        public async Task AddAsync(TKey key, TValue value)
        {
            if (!await DataCache.PutIfAbsentAsync(key, value).ConfigureAwait(false))
            {
                throw new ArgumentException("An element with the same key already exists in the dictionary");
            }
        }

        public async Task AddAsync(KeyValuePair<TKey, TValue> item)
        {
            if (!await DataCache.PutIfAbsentAsync(item.Key, item.Value).ConfigureAwait(false))
            {
                throw new ArgumentException("An element with the same key already exists in the dictionary");
            }
        }

        public void Clear() => DataCache.Clear();
        public async Task ClearAsync() => await DataCache.ClearAsync().ConfigureAwait(false);
        public bool Contains(KeyValuePair<TKey, TValue> item) => DataCache.AsCacheQueryable()
            .Any(x => x.Key!.Equals(item.Key) && x.Value!.Equals(item.Value));
        public async Task<bool> ContainsAsync(KeyValuePair<TKey, TValue> item) => await DataCache.AsCacheQueryable()
            .ToAsyncEnumerable().AnyAsync(x => x.Key!.Equals(item.Key) && x.Value!.Equals(item.Value))
            .ConfigureAwait(false);

        public bool ContainsKey(TKey key) => DataCache.ContainsKey(key);
        public Task<bool> ContainsKeyAsync(TKey key) => DataCache.ContainsKeyAsync(key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var kv in this)
            {
                array[arrayIndex++] = kv;
            }
        }

        public async IAsyncEnumerator<KeyValuePair<TKey, TValue>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            await foreach (var kv in DataCache.AsCacheQueryable().ToAsyncEnumerable())
            {
                yield return new KeyValuePair<TKey, TValue>(kv.Key, kv.Value);
            }
        }

        public async Task<int> GetCountAsync() => await DataCache
            .AsCacheQueryable()
            .ToAsyncEnumerable()
            .CountAsync()
            .ConfigureAwait(false);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => GetEnumerator();

        public async Task<ICollection<TKey>> GetKeysAsync() => await DataCache
            .AsCacheQueryable()
            .Select(x => x.Key)
            .ToAsyncEnumerable()
            .ToListAsync()
            .ConfigureAwait(false);

        public async Task<ICollection<TValue>> GetValuesAsync() => await DataCache
            .AsCacheQueryable()
            .Select(x => x.Value)
            .ToAsyncEnumerable()
            .ToListAsync()
            .ConfigureAwait(false);

        public bool Remove(TKey key) => DataCache.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => DataCache.Remove(item.Key, item.Value);
        public Task<bool> RemoveAsync(TKey key) => DataCache.RemoveAsync(key);
        public Task<bool> RemoveAsync(KeyValuePair<TKey, TValue> item) => DataCache.RemoveAsync(item.Key, item.Value);
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => DataCache.TryGet(key, out value);
        public async Task<(bool, TValue)> TryGetValueAsync(TKey key)
        {
            var result = await DataCache.TryGetAsync(key).ConfigureAwait(false);

            return (result.Success, result.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            foreach (var item in DataCache.AsCacheQueryable())
            {
                yield return item;
            }
        }

        public async Task<bool> SetIfNotExisting(TKey key, TValue value)
            => await DataCache.PutIfAbsentAsync(key, value).ConfigureAwait(false);

        public async Task<bool> SetIfNotChanged(TKey key, TValue oldValue, TValue newValue)
            => await DataCache.ReplaceAsync(key, oldValue, newValue).ConfigureAwait(false);

        private ICacheClient<TKey, TValue> DataCache
            => AsyncLocals.FabricService.GetDictionaryCache<TKey, TValue>(instance, name);

        public override string ToString() => $"PerperDictionary({Instance},{Name})";
    }
}
