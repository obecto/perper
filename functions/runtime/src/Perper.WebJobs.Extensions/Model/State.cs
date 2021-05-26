using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Apache.Ignite.Core.Client;

using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class State : IState
    {
        public string Agent => _instance.Agent;

        private readonly PerperInstanceData _instance;
        private readonly IIgniteClient _ignite;
        private readonly PerperBinarySerializer _serializer;

        [NonSerialized] public List<StateEntry> Entries = new List<StateEntry>();

        [PerperInject]
        public State(PerperInstanceData instance, IIgniteClient ignite, PerperBinarySerializer serializer)
        {
            _instance = instance;
            _ignite = ignite;
            _serializer = serializer;
        }

        public async Task<T> GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            var cache = _ignite.GetOrCreateCache<string, object>(Agent).WithKeepBinary<string, object>();
            var result = await cache.TryGetAsync(key);
            if (!result.Success)
            {
                var defaultValue = defaultValueFactory();
                await cache.PutAsync(key, _serializer.SerializeRoot(defaultValue));
                return defaultValue;
            }
            return (T)_serializer.DeserializeRoot(result.Value, typeof(T))!;
        }

        public Task SetValue<T>(string key, T value)
        {
            var cache = _ignite.GetOrCreateCache<string, object>(Agent).WithKeepBinary<string, object>();
            return cache.PutAsync(key, _serializer.SerializeRoot(value));
        }

        public async Task<IStateEntry<T>> Entry<T>(string key, Func<T> defaultValueFactory)
        {
            var entry = UnloadedEntry(key, defaultValueFactory);
            await entry.Load();
            return entry;
        }

        public StateEntry<T> UnloadedEntry<T>(string key, Func<T> defaultValueFactory) => new StateEntry<T>(this, key, defaultValueFactory);

        public Task LoadStateEntries() => Task.WhenAll(Entries.Select(x => x.Load()));

        public Task StoreStateEntries() => Task.WhenAll(Entries.Select(x => x.Store()));
    }
}