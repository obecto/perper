using System.Collections.Generic;
using System.Threading.Tasks;

namespace Perper.Extensions.Collections
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "We want define async dictionary")]
    public interface IAsyncDictionary<TKey, TValue> : IAsyncEnumerable<KeyValuePair<TKey, TValue>>
    {
        public string Name { get; }
        Task<ICollection<TKey>> GetKeysAsync();
        Task<ICollection<TValue>> GetValuesAsync();
        Task<int> GetCountAsync();
        Task AddAsync(TKey key, TValue value);
        Task AddAsync(KeyValuePair<TKey, TValue> item);
        Task ClearAsync();
        Task<bool> ContainsAsync(KeyValuePair<TKey, TValue> item);
        Task<bool> ContainsKeyAsync(TKey key);
        Task<bool> RemoveAsync(TKey key);
        Task<bool> RemoveAsync(KeyValuePair<TKey, TValue> item);
        Task<(bool, TValue)> TryGetValueAsync(TKey key);
        Task SetAsync(TKey key, TValue value);
        Task<bool> SetIfNotExistingAsync(TKey key, TValue value);
        Task<bool> SetIfNotChangedAsync(TKey key, TValue oldValue, TValue newValue);
    }
}