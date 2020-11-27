using System;
using System.Threading.Tasks;
using System.Runtime.Serialization;
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
        [PerperInject] protected IState State { // Used to ensure that State.Entries is updated correctly
            set {
                _state = value;
                ((State)_state).Entries.Add(this);
            }
        }

        [NonSerialized] public Func<T> DefaultValueFactory = () => {
            if (typeof(T).GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance<T>();
            }
            return default(T)!;
        };

        [NonSerialized] private IContext? _context;
        [NonSerialized] private string? _name;
        public string Name { get => _name ?? ((Context) _context!).InstanceName; set => _name = value; }

        [IgnoreDataMember]
        public T Value { get; set; } = default(T)!;

        // HACK: We cannot have multiple constructors, as the DI container ignores [ActivatorUtilitiesConstructor]
        // So we just pass Name/DefaultValueFactory directly.
        // Might be possible to rework with a proxy class/wrapper which is used for DI only
        public StateEntry(IState state, IContext? context)
        {
            State = state;
            _context = context;
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
}