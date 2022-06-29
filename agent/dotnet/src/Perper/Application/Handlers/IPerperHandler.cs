using System.Reflection;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public interface IPerperHandler
    {
        ParameterInfo[]? GetParameters();
    }

    public interface IPerperHandler<TResult> : IPerperHandler
    {
        Task<TResult> Invoke(PerperExecutionData executionData, object?[] arguments);
    }
}