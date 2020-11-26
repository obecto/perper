using System;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Perper.WebJobs.Extensions.Services;
using Microsoft.Extensions.DependencyInjection;

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

        [NonSerialized] private Func<T> _defaultValueFactory = () => {
            if (typeof(T).GetConstructor(Type.EmptyTypes) != null)
            {
                return Activator.CreateInstance<T>();
            }
            return default(T)!;
        };

        public string Name { get; private set; }

        [IgnoreDataMember]
        public T Value { get; set; } = default(T)!;

        [ActivatorUtilitiesConstructor]
        public StateEntry(IState state, IContext context)
        {
            Name = ((Context) context).InstanceName;
            State = state;
        }

        public StateEntry(IState state, string name, Func<T> defaultValueFactory)
        {
            Name = name;
            State = state;
            _defaultValueFactory = defaultValueFactory;
        }

        public override async Task Load()
        {
            Value = await _state.GetValue<T>(Name, _defaultValueFactory);
        }

        public override Task Store()
        {
            return _state.SetValue<T>(Name, Value);
        }
    }
}