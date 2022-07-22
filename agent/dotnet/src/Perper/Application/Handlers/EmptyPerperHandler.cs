using System.Reflection;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public class EmptyPerperHandler : IPerperHandler
    {
        public ParameterInfo[]? GetParameters() => null;

        public Task Invoke(PerperExecutionData executionData, object?[] arguments)
        {
            return Task.CompletedTask;
        }
    }
}