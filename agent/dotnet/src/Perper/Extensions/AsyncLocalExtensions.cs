using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Perper.Model;
using PerperStateModel = Perper.Model.PerperState;

namespace Perper.Extensions
{
    public static class AsyncLocalExtensions
    {
        public static IPerperContext PerperContext => AsyncLocalContext.PerperContext;

        public static IDisposable UseContext(this IPerperContext context) =>
            new AsyncLocalContext(context);

        public static Task<TResult> CallAsync<TResult>(this PerperAgent agent, string @delegate, params object?[] parameters) =>
            PerperContext.CallAsync<TResult>(agent, @delegate, parameters);

        public static Task CallAsync(this PerperAgent agent, string @delegate, params object?[] parameters) =>
            PerperContext.CallAsync(agent, @delegate, parameters);

        public static Task DestroyAsync(this PerperAgent agent) =>
            PerperContext.Agents.DestroyAsync(agent);

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

        public static Task<(bool Exists, T Value)> TryGetAsync<T>(this PerperStateModel state, string key) =>
            PerperContext.States.TryGetAsync<T>(state, key);
        public static Task SetAsync<T>(this PerperStateModel state, string key, T value) =>
            PerperContext.States.SetAsync(state, key, value);
        public static Task RemoveAsync(this PerperStateModel state, string key) =>
            PerperContext.States.RemoveAsync(state, key);
        public static Task<T> GetOrDefaultAsync<T>(this PerperStateModel state, string key, T @default = default!) =>
            PerperContext.States.GetOrDefaultAsync(state, key, @default);
        public static Task<T> GetOrNewAsync<T>(this PerperStateModel state, string key) where T : new() =>
            PerperContext.States.GetOrNewAsync<T>(state, key);
        public static Task<T> GetOrNewAsync<T>(this PerperStateModel state, string key, Func<T> createFunc) =>
            PerperContext.States.GetOrNewAsync(state, key, createFunc);
    }
}