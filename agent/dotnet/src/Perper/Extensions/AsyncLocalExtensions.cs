using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public static class AsyncLocalExtensions
    {
        public static IPerperContext PerperContext => AsyncLocalContext.PerperContext;

        public static IDisposable UseContext(this IPerperContext context) =>
            new AsyncLocalContext(context);

        #region Agents
        public static Task<TResult> CallAsync<TResult>(this PerperInstance agent, string @delegate, params object?[] parameters) =>
            PerperContext.CallAsync<TResult>(agent, @delegate, parameters);

        public static Task CallAsync(this PerperInstance agent, string @delegate, params object?[] parameters) =>
            PerperContext.CallAsync(agent, @delegate, parameters);

        public static Task DestroyAsync(this PerperInstance agent) =>
            PerperContext.Agents.DestroyAsync(agent);
        #endregion Agents

        #region Streams
        public static IAsyncEnumerable<(long, TItem)> EnumerateItemsAsync<TItem>(this PerperStream stream, string? listenerName = null, CancellationToken cancellationToken = default) =>
            PerperContext.EnumerateItemsAsync<TItem>(stream, listenerName, cancellationToken);

        public static IAsyncEnumerable<TItem> EnumerateAsync<TItem>(this PerperStream stream, string? listenerName = null, CancellationToken cancellationToken = default) =>
            PerperContext.EnumerateAsync<TItem>(stream, listenerName, cancellationToken);

        public static IQueryable<TItem> Query<TItem>(this PerperStream stream) =>
            PerperContext.Streams.Query<TItem>(stream);

        public static IAsyncEnumerable<TItem> QuerySqlAsync<TItem>(this PerperStream stream, string sqlCondition, params object[] sqlParameters) =>
            PerperContext.Streams.QuerySqlAsync<TItem>(stream, sqlCondition, sqlParameters);

        public static Task DestroyAsync(this PerperStream stream) =>
            PerperContext.Streams.DestroyAsync(stream);

        public static Task WriteItemAsync<TItem>(this PerperStream stream, long key, TItem item) =>
            PerperContext.Streams.WriteItemAsync(stream, key, item);

        public static Task WriteItemAsync<TItem>(this PerperStream stream, TItem item) =>
            PerperContext.Streams.WriteItemAsync(stream, item);
        #endregion Streams

        #region Dictionaries
        #region DictionariesString
        public static Task<(bool Exists, T Value)> TryGetAsync<T>(this PerperDictionary dictionary, string key) =>
            PerperContext.States.TryGetAsync<string, T>(dictionary, key);

        public static Task SetAsync<T>(this PerperDictionary dictionary, string key, T value) =>
            PerperContext.States.SetAsync(dictionary, key, value);

        public static Task RemoveAsync(this PerperDictionary dictionary, string key) =>
            PerperContext.States.RemoveAsync(dictionary, key);

        public static Task<T> GetOrDefaultAsync<T>(this PerperDictionary dictionary, string key, T @default = default!) =>
            PerperContext.States.GetOrDefaultAsync(dictionary, key, @default);

        public static Task<T> GetOrNewAsync<T>(this PerperDictionary dictionary, string key) where T : new() =>
            PerperContext.States.GetOrNewAsync<string, T>(dictionary, key);

        public static Task<T> GetOrNewAsync<T>(this PerperDictionary dictionary, string key, Func<T> createFunc) =>
            PerperContext.States.GetOrNewAsync(dictionary, key, createFunc);
        #endregion DictionariesString

        public static Task SetAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key, TValue value) =>
            PerperContext.States.SetAsync(dictionary, key, value);
        public static Task<bool> SetIfNotChangedAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key, TValue oldValue, TValue value) =>
            PerperContext.States.SetIfNotChangedAsync(dictionary, key, oldValue, value);
        public static Task<bool> SetIfNotExistingAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key, TValue value) =>
            PerperContext.States.SetIfNotExistingAsync(dictionary, key, value);

        public static Task<(bool Exists, TValue Value)> TryGetAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key) =>
            PerperContext.States.TryGetAsync<TKey, TValue>(dictionary, key);

        public static Task<TValue> GetOrDefaultAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key, TValue @default = default!) =>
            PerperContext.States.GetOrDefaultAsync(dictionary, key, @default);

        public static Task<TValue> GetOrNewAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key) where TValue : new() =>
            PerperContext.States.GetOrNewAsync<TKey, TValue>(dictionary, key);

        public static Task<TValue> GetOrNewAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key, Func<TValue> createFunc) =>
            PerperContext.States.GetOrNewAsync(dictionary, key, createFunc);

        public static Task<(bool Exists, TValue Value)> TryGetAndReplaceAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key, TValue newValue) =>
            PerperContext.States.TryGetAndReplaceAsync(dictionary, key, newValue);

        public static Task<(bool Exists, TValue Value)> TryGetAndRemoveAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key) =>
            PerperContext.States.TryGetAndRemoveAsync<TKey, TValue>(dictionary, key);

        public static Task<bool> RemoveAsync<TKey>(this PerperDictionary dictionary, TKey key) =>
            PerperContext.States.RemoveAsync(dictionary, key);
        public static Task<bool> RemoveIfNotChangedAsync<TKey, TValue>(this PerperDictionary dictionary, TKey key, TValue oldValue) =>
            PerperContext.States.RemoveIfNotChangedAsync(dictionary, key, oldValue);

        public static Task<bool> ContainsKeyAsync<TKey>(this PerperDictionary dictionary, TKey key) =>
            PerperContext.States.ContainsKeyAsync(dictionary, key);

        public static IAsyncEnumerable<(TKey Key, TValue Value)> EnumerateItemsAsync<TKey, TValue>(this PerperDictionary dictionary) =>
            PerperContext.States.EnumerateItemsAsync<TKey, TValue>(dictionary);

        public static Task<int> CountAsync(this PerperDictionary dictionary) =>
            PerperContext.States.CountAsync(dictionary);

        public static Task ClearAsync(this PerperDictionary dictionary) =>
            PerperContext.States.ClearAsync(dictionary);

        public static Task DestroyAsync(this PerperDictionary dictionary) =>
            PerperContext.States.DestroyAsync(dictionary);
        #endregion Dictionaries

        #region Lists
        public static IAsyncEnumerable<TValue> EnumerateAsync<TValue>(this PerperList list) =>
            PerperContext.States.EnumerateAsync<TValue>(list);

        public static Task<int> CountAsync(this PerperList list) =>
            PerperContext.States.CountAsync(list);

        public static Task AddAsync<TValue>(this PerperList list, TValue value) =>
            PerperContext.States.AddAsync(list, value);

        public static Task<TValue> PopAsync<TValue>(this PerperList list) =>
            PerperContext.States.PopAsync<TValue>(list);

        public static Task<TValue> DequeueAsync<TValue>(this PerperList list) =>
            PerperContext.States.DequeueAsync<TValue>(list);

        public static Task InsertAtAsync<TValue>(this PerperList list, int atIndex, TValue value) =>
        PerperContext.States.InsertAtAsync(list, atIndex, value);

        public static Task RemoveAtAsync(this PerperList list, int atIndex) =>
        PerperContext.States.RemoveAtAsync(list, atIndex);

        public static Task<TValue> GetAtAsync<TValue>(this PerperList list, int atIndex) =>
        PerperContext.States.GetAtAsync<TValue>(list, atIndex);

        public static Task SetAtAsync<TValue>(this PerperList list, int atIndex, TValue value) =>
        PerperContext.States.SetAtAsync(list, atIndex, value);

        public static Task<int> IndexOfAsync<TValue>(this PerperList list, TValue value) =>
        PerperContext.States.IndexOfAsync(list, value);

        public static Task<bool> ContainsAsync<TValue>(this PerperList list, TValue value) =>
        PerperContext.States.ContainsAsync(list, value);

        public static Task<bool> RemoveAsync<TValue>(this PerperList list, TValue value) =>
            PerperContext.States.RemoveAsync(list, value);

        public static Task ClearAsync(this PerperList list) =>
            PerperContext.States.ClearAsync(list);

        public static Task DestroyAsync(this PerperList list) =>
            PerperContext.States.DestroyAsync(list);
        #endregion Lists
    }
}