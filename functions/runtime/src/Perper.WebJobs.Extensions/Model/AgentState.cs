using System;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;

namespace Perper.WebJobs.Extensions.Model
{
    public class AgentState : IAgentState
    {
        public string CacheName { get; set; }

        [NonSerialized] private IIgniteClient _ignite;

        public async Task<T> GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            var cache = _ignite.GetOrCreateCache<string, T>(CacheName);
            var result = await cache.TryGetAsync(key);
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

        public IAgentState GetSubState(string name)
        {
            var subCacheName = CacheName + ':' + name;
            return new AgentState { CacheName = subCacheName };
        }
    }
}