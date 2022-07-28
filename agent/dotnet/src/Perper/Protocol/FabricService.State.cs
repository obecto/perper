using System.Threading.Tasks;

using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Client.Cache;

using Perper.Model;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperStates
    {
        PerperState IPerperStates.Create() => new(GenerateName(""));
        PerperState IPerperStates.Create(PerperAgent instance, string? name) => new($"{instance.Instance}-{name}");
        PerperState IPerperStates.Create(PerperExecution execution, string? name) => new($"{execution.Execution}-{name}");

        async Task<(bool Exists, TValue Value)> IPerperStates.TryGetAsync<TValue>(PerperState state, string key)
        {
            var result = await GetStateCache<string, object>(state).TryGetAsync(key).ConfigureAwait(false);
            if (!result.Success)
            {
                return (false, default(TValue)!);
            }
            return (true, (TValue)result.Value!);
        }

        async Task IPerperStates.SetAsync<TValue>(PerperState state, string key, TValue value)
        {
            if (value != null)
            {
                await GetStateCache<string, object>(state).PutAsync(key, value).ConfigureAwait(false);
            }
            else
            {
                await GetStateCache<string, object>(state).RemoveAsync(key).ConfigureAwait(false);
            }
        }

        async Task IPerperStates.RemoveAsync(PerperState state, string key) => await GetStateCache<string, object>(state).RemoveAsync(key).ConfigureAwait(false);

        async Task IPerperStates.DestroyAsync(PerperState state) => await Task.Run(() => Ignite.DestroyCache(state.Name)).ConfigureAwait(false);

        IAsyncList<T> IPerperStates.AsAsyncList<T>(PerperState state)
        {
            return new FabricList<T>(this, state);
        }

        IAsyncDictionary<TK, TV> IPerperStates.AsAsyncDictionary<TK, TV>(PerperState state)
        {
            return new FabricDictionary<TK, TV>(this, state);
        }

        public ICacheClient<TK, TV> GetStateCache<TK, TV>(PerperState state)
        {
            var queryEntity = new QueryEntity(typeof(TK), typeof(TV));

            if (queryEntity.Fields == null)
            {
                return Ignite.GetOrCreateCache<TK, TV>(state.Name).WithKeepBinary(FabricCaster.TypeShouldKeepBinary(typeof(TV)));
            }
            else
            {
                return Ignite.GetOrCreateCache<TK, TV>(new CacheClientConfiguration(state.Name, queryEntity)).WithKeepBinary(FabricCaster.TypeShouldKeepBinary(typeof(TV)));
            }
        }
    }
}