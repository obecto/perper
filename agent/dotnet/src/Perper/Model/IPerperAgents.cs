using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IPerperAgents
    {
        (PerperAgent Instance, DelayedCreateFunc Start) Create(PerperAgent? parent, string agent);
        (PerperAgent Instance, DelayedCreateFunc<TResult> Start) Create<TResult>(PerperAgent? parent, string agent);

        Task DestroyAsync(PerperAgent agent);
    }
}