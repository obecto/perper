using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeState : IState
    {
        public ConcurrentDictionary<string, object?> Values { get; } = new ConcurrentDictionary<string, object?>();

        // FIXME: Rename methods in state to Async?
        Task<T> IState.GetValue<T>(string key, Func<T> defaultValueFactory) => Task.FromResult(GetValue(key, defaultValueFactory));

        Task IState.SetValue<T>(string key, T value)
        {
            SetValue(key, value);
            return Task.CompletedTask;
        }

        Task<IStateEntry<T>> IState.Entry<T>(string key, Func<T> defaultValueFactory) => Task.FromResult(Entry(key, defaultValueFactory));

        public IStateEntry<T> Entry<T>(string key, Func<T> defaultValueFactory) => new FakeStateEntry<T>(this, key, defaultValueFactory, false);

        public T GetValue<T>(string key) => FakeConfiguration.Deserialize<T>(Values[key]);

        public T GetValue<T>(string key, Func<T> defaultValueFactory) => FakeConfiguration.Deserialize<T>(Values.GetOrAdd(key, _k => FakeConfiguration.Serialize(defaultValueFactory())));

        public void SetValue<T>(string key, T value) => Values[key] = FakeConfiguration.Serialize(value);
    }
}