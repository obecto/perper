using System.Reflection;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public interface IPerperHandler
    {
        ParameterInfo[]? GetParameters();
        Task Invoke(PerperExecutionData executionData, object?[] arguments);
    }
}