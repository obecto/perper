using System;
using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperStatesExtensions
    {
        public static Task<T> GetOrDefaultAsync<T>(this IPerperStates states, PerperState state, string key, T @default = default!) =>
            states.GetOrNewAsync(state, key, () => @default);

        public static Task<T> GetOrNewAsync<T>(this IPerperStates states, PerperState state, string key) where T : new() =>
            states.GetOrNewAsync(state, key, () => new T());

        public static async Task<T> GetOrNewAsync<T>(this IPerperStates states, PerperState state, string key, Func<T> createFunc)
        {
            var (success, value) = await states.TryGetAsync<T>(state, key).ConfigureAwait(false);
            return success ? value : createFunc();
        }
    }
}