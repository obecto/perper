using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IPerperStates
    {
        #region Dictionaries
        (PerperDictionary Dictionary, Func<Task> Create) CreateDictionary(PerperStateOptions? options = null);
        PerperDictionary GetInstanceDictionary(PerperInstance instance);

        Task SetAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue value);
        Task<bool> SetIfNotChangedAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue oldValue, TValue value);
        Task<bool> SetIfNotExistingAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue value);

        Task<(bool Exists, TValue Value)> TryGetAsync<TKey, TValue>(PerperDictionary dictionary, TKey key);
        Task<(bool Exists, TValue Value)> TryGetAndReplaceAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue newValue);
        Task<(bool Exists, TValue Value)> TryGetAndRemoveAsync<TKey, TValue>(PerperDictionary dictionary, TKey key);

        Task<bool> RemoveAsync<TKey>(PerperDictionary dictionary, TKey key);
        Task<bool> RemoveIfNotChangedAsync<TKey, TValue>(PerperDictionary dictionary, TKey key, TValue oldValue);

        Task<bool> ContainsKeyAsync<TKey>(PerperDictionary dictionary, TKey key);

        IAsyncEnumerable<(TKey Key, TValue Value)> EnumerateItemsAsync<TKey, TValue>(PerperDictionary dictionary);

        Task<int> CountAsync(PerperDictionary dictionary);

        Task ClearAsync(PerperDictionary dictionary);

        Task DestroyAsync(PerperDictionary dictionary);
        #endregion Dictionaries

        #region Lists
        (PerperList List, Func<Task> Create) CreateList(PerperStateOptions? options = null);
        PerperList GetInstanceChildrenList(PerperInstance instance);

        IAsyncEnumerable<TValue> EnumerateAsync<TValue>(PerperList list);
        Task<int> CountAsync(PerperList list);

        Task AddAsync<TValue>(PerperList list, TValue value);
        Task<TValue> PopAsync<TValue>(PerperList list);
        Task<TValue> DequeueAsync<TValue>(PerperList list);

        Task InsertAtAsync<TValue>(PerperList list, int atIndex, TValue value);
        Task RemoveAtAsync(PerperList list, int atIndex);
        Task<TValue> GetAtAsync<TValue>(PerperList list, int atIndex);
        Task SetAtAsync<TValue>(PerperList list, int atIndex, TValue value);
        Task<int> IndexOfAsync<TValue>(PerperList list, TValue value);

        Task<bool> RemoveAsync<TValue>(PerperList list, TValue value);

        Task ClearAsync(PerperList list);

        Task DestroyAsync(PerperList list);
        #endregion Lists
    }
}