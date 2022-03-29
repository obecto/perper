using System.Threading.Tasks;

namespace Perper.Model
{
    public interface IPerperAgents
    {
        (PerperAgent Instance, DelayedCreateFunc Start) Create(string agent);
        (PerperAgent Instance, DelayedCreateFunc<TResult> Start) Create<TResult>(string agent);

        Task DestroyAsync(PerperAgent agent);
    }
}