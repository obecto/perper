using System;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Fake
{
    public class FakeStateEntry<T> : IStateEntry<T>
    {
        public T Value { get; set; }
        public Func<T> DefaultValueFactory { get; }
        public string Name { get; }
        public FakeState State { get; }

        public FakeStateEntry(FakeState? state = null, string? name = null, Func<T>? defaultValueFactory = null, bool unloaded = false)
        {
            State = state ?? new FakeState();
            Name = name ?? Guid.NewGuid().ToString();
            DefaultValueFactory = defaultValueFactory ?? (Func<T>)(() => default(T)!);
            Value = unloaded ? default(T)! : State.GetValue<T>(Name, DefaultValueFactory);
        }

        public async Task Load()
        {
            Value = await ((IState)State).GetValue<T>(Name, DefaultValueFactory);
        }

        public Task Store()
        {
            return ((IState)State).SetValue<T>(Name, Value);
        }
    }
}