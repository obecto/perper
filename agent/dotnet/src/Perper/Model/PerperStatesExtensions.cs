using System;
using System.Threading.Tasks;

namespace Perper.Model
{
    public static class PerperStatesExtensions
    {
        public static async Task<PerperDictionary> CreateDictionaryAsync(this IPerperStates perperStates, PerperStateOptions? options = null)
        {
            var (dictionary, createAsync) = perperStates.CreateDictionary(options);
            await createAsync().ConfigureAwait(false);
            return dictionary;
        }
        public static async Task<PerperList> CreateListAsync(this IPerperStates perperStates, PerperStateOptions? options = null)
        {
            var (list, createAsync) = perperStates.CreateList(options);
            await createAsync().ConfigureAwait(false);
            return list;
        }

        public static Task<TValue> GetOrDefaultAsync<TKey, TValue>(this IPerperStates states, PerperDictionary dictionary, TKey key, TValue @default = default!) =>
            states.GetOrNewAsync(dictionary, key, () => @default);

        public static Task<TValue> GetOrNewAsync<TKey, TValue>(this IPerperStates states, PerperDictionary dictionary, TKey key) where TValue : new() =>
            states.GetOrNewAsync(dictionary, key, () => new TValue());

        public static async Task<TValue> GetOrNewAsync<TKey, TValue>(this IPerperStates states, PerperDictionary dictionary, TKey key, Func<TValue> createFunc)
        {
            var (success, value) = await states.TryGetAsync<TKey, TValue>(dictionary, key).ConfigureAwait(false);
            return success ? value : createFunc();
        }

        public static async Task<bool> ContainsAsync<TValuealue>(this IPerperStates states, PerperList list, TValuealue value)
        {
            return await states.IndexOfAsync(list, value).ConfigureAwait(false) != -1;
        }
    }
}