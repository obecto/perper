using System.Reflection;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public class EmptyPerperHandler : IPerperHandler<VoidStruct>
    {
        public ParameterInfo[]? GetParameters() => null;

        public Task<VoidStruct> Invoke(PerperExecutionData executionData, object?[] arguments)
        {
            return Task.FromResult(VoidStruct.Value);
        }
    }
}