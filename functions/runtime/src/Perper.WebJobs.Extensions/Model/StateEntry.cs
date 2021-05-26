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
        [NonSerialized] private readonly State _state;
        [NonSerialized] public Func<T> DefaultValueFactory = () => default!;

        [IgnoreDataMember]
        public T Value { get; set; } = default!;

        public string Name { get; private set; }

#pragma warning disable CS8618
        [PerperInject]
        protected StateEntry(IState state)
        {
            _state = (State)state;
            _state.Entries.Add(this);
        }
#pragma warning restore CS8618

        public StateEntry(IState state, string name, Func<T> defaultValueFactory)
            : this(state)
        {
            Name = name;
            DefaultValueFactory = defaultValueFactory;
        }

        public override async Task Load() => Value = await _state.GetValue(Name, DefaultValueFactory);

        public override Task Store() => _state.SetValue(Name, Value);
    }

    public class StateEntryDI<T> : IStateEntry<T>
    {
        protected IStateEntry<T>? _implementation;
        [NonSerialized] protected IState _state;
        [NonSerialized] protected PerperInstanceData _instance;

        [PerperInject]
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
                    return default!;
                });
            }
            return _implementation;
        }
    }
}