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
        [PerperInject] protected PerperInstanceData _instance;
        [PerperInject] protected IIgniteClient _ignite;
        [PerperInject] protected PerperBinarySerializer _serializer;

        [NonSerialized] public List<StateEntry> Entries = new List<StateEntry>();

        public State(PerperInstanceData instance, IIgniteClient ignite, PerperBinarySerializer serializer)
        {
            _instance = instance;
            _ignite = ignite;
            _serializer = serializer;
        }

        public async Task<T> GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            var converters = _serializer.GetRootObjectConverters(typeof(T));

            var cache = _ignite.GetOrCreateCache<string, object>(_instance.InstanceData.Agent);
            var result = await cache.TryGetAsync(key);
            if (!result.Success)
            {
                var defaultValue = defaultValueFactory();
                await cache.PutAsync(key, converters.to.Invoke(defaultValue));
                return defaultValue;
            }
            return (T)converters.from.Invoke(result.Value)!;
        }

        public Task SetValue<T>(string key, T value)
        {
            var converters = _serializer.GetRootObjectConverters(typeof(T));

            var cache = _ignite.GetOrCreateCache<string, object>(_instance.InstanceData.Agent);
            return cache.PutAsync(key, converters.to.Invoke(value));
        }

        public async Task<IStateEntry<T>> Entry<T>(string key, Func<T> defaultValueFactory)
        {
            var entry = UnloadedEntry(key, defaultValueFactory);
            await entry.Load();
            return entry;
        }

        public StateEntry<T> UnloadedEntry<T>(string key, Func<T> defaultValueFactory)
        {
            return new StateEntry<T>(this, key, defaultValueFactory);
        }

        public Task LoadStateEntries()
        {
            return Task.WhenAll(Entries.Select(x => x.Load()));
        }

        public Task StoreStateEntries()
        {
            return Task.WhenAll(Entries.Select(x => x.Store()));
        }
    }
}