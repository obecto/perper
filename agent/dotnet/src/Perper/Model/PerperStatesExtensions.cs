using System;
using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperStatesExtensions
    {
        public static async Task<PerperState> CreateAsync(this IPerperStates perperStates, PerperStateOptions? options = null)
        {
            var (state, createAsync) = perperStates.Create(options);
            await createAsync().ConfigureAwait(false);
            return state;
        }

        public static async Task<PerperState> CreateAsync(this IPerperStates perperStates, PerperAgent instance, string? name = null, PerperStateOptions? options = null)
        {
            var (state, createAsync) = perperStates.Create(instance, name, options);
            await createAsync().ConfigureAwait(false);
            return state;
        }

        public static async Task<PerperState> CreateAsync(this IPerperStates perperStates, PerperExecution execution, string? name = null, PerperStateOptions? options = null)
        {
            var (state, createAsync) = perperStates.Create(execution, name, options);
            await createAsync().ConfigureAwait(false);
            return state;
        }

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