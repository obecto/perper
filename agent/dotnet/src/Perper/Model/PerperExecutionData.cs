using System.Threading;

namespace Perper.Model
{
    public record PerperExecutionData(
        PerperInstance Agent,
        string Delegate,
        PerperExecution Execution,
        object?[] Arguments,
        CancellationToken CancellationToken)
    {
        public bool IsSynthetic { init; get; }
    }
}