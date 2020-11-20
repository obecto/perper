using System;
using System.Threading.Tasks;

namespace Perper.WebJobs.Extensions.Model
{
    public abstract class StateEntry
    {
        public abstract Task Load();
        public abstract Task Store();
    }

    public class StateEntry<T> : StateEntry, IStateEntry<T>
    {
        [NonSerialized] private IState _state;
        [NonSerialized] private string _name;
        [NonSerialized] private Func<T> _defaultValueFactory;

        public T Value { get; set; } = default(T)!;

        public StateEntry(IState state, string name, Func<T> defaultValueFactory)
        {
            _name = name;
            _state = state;
            _defaultValueFactory = defaultValueFactory;
        }

        public override async Task Load()
        {
            Value = await _state.GetValue<T>(_name, _defaultValueFactory);
        }

        public override Task Store()
        {
            return _state.SetValue<T>(_name, Value);
        }
    }
}