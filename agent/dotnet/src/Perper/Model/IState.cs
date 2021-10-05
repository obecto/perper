using System;
using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IState
    {
        Task<(bool, T)> TryGetAsync<T>(string key);

        [Obsolete("Use idempotent operations instead")]
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> defaultValueFactory);

        Task SetAsync<T>(string key, T value);

        async Task<T> GetOrDefaultAsync<T>(string key, T empty = default) => (await TryGetAsync<T>(key).ConfigureAwait(false)) is (true, var result) ? result : empty;
        async Task<T> GetOrNewAsync<T>(string key) where T : new() => (await TryGetAsync<T>(key).ConfigureAwait(false)) is (true, var result) ? result : new T();
    }
}