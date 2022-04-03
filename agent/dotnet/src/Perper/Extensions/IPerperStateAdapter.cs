using System;
using System.Threading.Tasks;

namespace Perper.Extensions
{
#pragma warning disable CA1716
    public interface IPerperStateAdapter
    {
        Task<(bool Exists, T Value)> TryGetAsync<T>(string key) => AsyncLocalContext.PerperContext.CurrentState.TryGetAsync<T>(key);
        Task SetAsync<T>(string key, T value) => AsyncLocalContext.PerperContext.CurrentState.SetAsync(key, value);
        Task RemoveAsync(string key) => AsyncLocalContext.PerperContext.CurrentState.RemoveAsync(key);
        Task<T> GetOrDefaultAsync<T>(string key, T @default = default!) => AsyncLocalContext.PerperContext.CurrentState.GetOrDefaultAsync(key, @default);
        Task<T> GetOrNewAsync<T>(string key) where T : new() => AsyncLocalContext.PerperContext.CurrentState.GetOrNewAsync<T>(key);
        Task<T> GetOrNewAsync<T>(string key, Func<T> createFunc) => AsyncLocalContext.PerperContext.CurrentState.GetOrNewAsync(key, createFunc);
    }
#pragma warning restore CA1716
}