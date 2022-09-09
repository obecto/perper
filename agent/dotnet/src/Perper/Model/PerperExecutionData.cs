using System.Threading;

namespace Perper.Model
{
    public record PerperExecutionData(
        PerperAgent Agent,
        string Delegate,
        PerperExecution Execution,
        CancellationToken CancellationToken)
    {
        public bool IsSynthetic { init; get; }
        public PerperState? State { init; get; }
    }
}