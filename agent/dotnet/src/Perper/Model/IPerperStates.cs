using System;
using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IPerperStates
    {
        (PerperState State, Func<Task> Create) Create(PerperStateOptions? options = null);
        (PerperState State, Func<Task> Create) Create(PerperAgent instance, string? name = null, PerperStateOptions? options = null);
        (PerperState State, Func<Task> Create) Create(PerperExecution execution, string? name = null, PerperStateOptions? options = null);

        Task<(bool Exists, TValue Value)> TryGetAsync<TValue>(PerperState state, string key);
        Task SetAsync<TValue>(PerperState state, string key, TValue value);
        Task RemoveAsync(PerperState state, string key);

        IAsyncList<T> AsAsyncList<T>(PerperState state);
        IAsyncDictionary<TK, TV> AsAsyncDictionary<TK, TV>(PerperState state);

        Task DestroyAsync(PerperState state);
    }
}