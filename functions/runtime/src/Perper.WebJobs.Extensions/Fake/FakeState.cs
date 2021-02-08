using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeState : IState
    {
        public ConcurrentDictionary<string, object?> Values { get; } = new ConcurrentDictionary<string, object?>();

        public class TestStateEntry<T> : IStateEntry<T>
        {
            public T Value { get; set; } = default(T)!;

            public Func<T> DefaultValueFactory = () => default(T)!;
            public string Name { get; private set; }

            private IState _instance;

            public TestStateEntry(IState instance, string name, Func<T> defaultValueFactory)
            {
                _instance = instance;
                Name = name;
                DefaultValueFactory = defaultValueFactory;
            }

            public async Task Load()
            {
                Value = await _instance.GetValue<T>(Name, DefaultValueFactory);
            }

            public Task Store()
            {
                return _instance.SetValue<T>(Name, Value);
            }
        }

        Task<T> IState.GetValue<T>(string key, Func<T> defaultValueFactory) // FIXME: Rename methods in state to Async?
        {
            return Task.FromResult(GetValue<T>(key, defaultValueFactory));
        }

        Task IState.SetValue<T>(string key, T value)
        {
            SetValue<T>(key, value);
            return Task.CompletedTask;
        }

        async Task<IStateEntry<T>> IState.Entry<T>(string key, Func<T> defaultValueFactory)
        {
            var entry = new TestStateEntry<T>(this, key, defaultValueFactory);
            await entry.Load();
            return entry;
        }

        public T GetValue<T>(string key)
        {
            return FakeConfiguration.Deserialize<T>(Values[key]);
        }

        public T GetValue<T>(string key, Func<T> defaultValueFactory)
        {
            return FakeConfiguration.Deserialize<T>(Values.GetOrAdd(key, _k => FakeConfiguration.Serialize(defaultValueFactory())));
        }

        public void SetValue<T>(string key, T value)
        {
            Values[key] = FakeConfiguration.Serialize(value);
        }
    }
}