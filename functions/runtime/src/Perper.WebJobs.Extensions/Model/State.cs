using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class State : IState
    {
        public string CacheName { get; }

        [PerperInject] private IIgniteClient _ignite;
        [PerperInject] private IServiceProvider _services;

        [NonSerialized] private List<StateEntry> _entries = new List<StateEntry>();

        public State(IContext context, IIgniteClient ignite, IServiceProvider services)
        {
            CacheName = ((Context) context).InstanceName;
            _services = services;
            _ignite = ignite;
        }

        public async Task<T> GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            var cache = _ignite.GetOrCreateCache<string, T>(CacheName);
            var result = await cache.TryGetWithServicesAsync(key, _services);
            if (!result.Success)
            {
                var defaultValue = defaultValueFactory();
                await cache.PutAsync(key, defaultValue);
                return defaultValue;
            }
            return result.Value;
        }

        public Task SetValue<T>(string key, T value)
        {
            var cache = _ignite.GetOrCreateCache<string, T>(CacheName);
            return cache.PutAsync(key, value);
        }

        public async Task<IStateEntry<T>> Entry<T>(string key, Func<T> defaultValueFactory)
        {
            var entry = UnloadedEntry(key, defaultValueFactory);
            await entry.Load();
            return entry;
        }

        public StateEntry<T> UnloadedEntry<T>(string key, Func<T> defaultValueFactory)
        {
            var entry = new StateEntry<T>(this, key, defaultValueFactory);
            _entries.Add(entry);
            return entry;
        }

        public Task LoadStateEntries()
        {
            return Task.WhenAll(_entries.Select(x => x.Load()));
        }

        public Task StoreStateEntries()
        {
            return Task.WhenAll(_entries.Select(x => x.Store()));
        }
    }
}