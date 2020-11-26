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
        private Context context { get => ((Context) _context); }

        [PerperInject] protected IContext _context;
        [PerperInject] protected IIgniteClient _ignite;
        [PerperInject] protected IServiceProvider _services;

        [NonSerialized] public List<StateEntry> Entries = new List<StateEntry>();

        public State(IContext context, IIgniteClient ignite, IServiceProvider services)
        {
            _context = context;
            _ignite = ignite;
            _services = services;
        }

        public async Task<T> GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            var cache = _ignite.GetOrCreateCache<string, T>(context.AgentName);
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
            var cache = _ignite.GetOrCreateCache<string, T>(context.AgentName);
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