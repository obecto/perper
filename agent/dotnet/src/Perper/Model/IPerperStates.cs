using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IPerperStates
    {
        PerperState Create();
        PerperState Create(PerperAgent instance, string? name = null);
        PerperState Create(PerperExecution execution, string? name = null);

        Task<(bool Exists, TValue Value)> TryGetAsync<TValue>(PerperState state, string key);
        Task SetAsync<TValue>(PerperState state, string key, TValue value);
        Task RemoveAsync(PerperState state, string key);

        Task DestroyAsync(PerperState state);
    }
}