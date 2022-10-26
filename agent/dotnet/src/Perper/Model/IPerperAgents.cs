using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IPerperAgents
    {
        (PerperInstance Instance, DelayedCreateFunc Start) Create(PerperInstance? parent, string agent);
        (PerperInstance Instance, DelayedCreateFunc<TResult> Start) Create<TResult>(PerperInstance? parent, string agent);

        Task DestroyAsync(PerperInstance agent);
    }
}