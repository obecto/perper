using System;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public static class PerperState
    {
        public static PerperDictionary State => AsyncLocalContext.PerperContext.CurrentState;

        public static Task<(bool Exists, T Value)> TryGetAsync<T>(string key) =>
            State.TryGetAsync<T>(key);
        public static Task SetAsync<T>(string key, T value) =>
            State.SetAsync(key, value);
        public static Task RemoveAsync(string key) =>
            State.RemoveAsync(key);
        public static Task<T> GetOrDefaultAsync<T>(string key, T defaultValue = default!) =>
            State.GetOrDefaultAsync(key, defaultValue);
        public static Task<T> GetOrNewAsync<T>(string key) where T : new() =>
            State.GetOrNewAsync<T>(key);
        public static Task<T> GetOrNewAsync<T>(string key, Func<T> createFunc) =>
            State.GetOrNewAsync(key, createFunc);

        /*public static ICacheClient<TK, TV> GetCacheClient<TK, TV>(string cache) =>
            ((FabricService)AsyncLocalContext.PerperContext.States).GetStateCache<TK, TV>(AsyncLocalContext.PerperContext.States.Create(PerperContext.CurrentAgent, cache));*/
    }
}