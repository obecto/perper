using System.Threading.Tasks;

namespace Perper.Model
{
    public delegate Task DelayedCreateFunc(params object?[] parameters);

    public delegate Task<TResult> DelayedCreateFunc<TResult>(params object?[] parameters);
}