using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public abstract class StateEntry
    {
        public abstract Task Load();
        public abstract Task Store();
    }

    public class StateEntry<T> : StateEntry, IStateEntry<T>
    {
        [NonSerialized] private IState _state = default!;
        [PerperInject]
        protected IState State
        { // Used to ensure that State.Entries is updated correctly
            set
            {
                _state = value;
                ((State)_state).Entries.Add(this);
            }
        }

        [NonSerialized] public Func<T> DefaultValueFactory = () => default(T)!;

        public string Name { get; private set; }

        [IgnoreDataMember]
        public T Value { get; set; } = default(T)!;

        public StateEntry(IState state, string name, Func<T> defaultValueFactory)
        {
            State = state;
            Name = name;
            DefaultValueFactory = defaultValueFactory;
        }

        public override async Task Load()
        {
            Value = await _state.GetValue<T>(Name, DefaultValueFactory);
        }

        public override Task Store()
        {
            return _state.SetValue<T>(Name, Value);
        }
    }

    public class StateEntryDI<T> : IStateEntry<T>
    {
        [NonSerialized] private IStateEntry<T>? _implementation;
        [PerperInject] protected IState _state;
        [PerperInject] protected PerperInstanceData _instance;

        public StateEntryDI(IState state, PerperInstanceData instance)
        {
            _state = state;
            _instance = instance;
        }

        [IgnoreDataMember]
        public T Value { get => GetImplementation().Value; set => GetImplementation().Value = value; }

        public Task Load() => GetImplementation().Load();
        public Task Store() => GetImplementation().Store();

        private IStateEntry<T> GetImplementation()
        {
            if (_implementation == null)
            {
                _implementation = new StateEntry<T>(_state, _instance.InstanceName, () =>
                {
                    if (typeof(T).GetConstructor(Type.EmptyTypes) != null)
                    {
                        return Activator.CreateInstance<T>();
                    }
                    return default(T)!;
                });
            }
            return _implementation;
        }
    }
}