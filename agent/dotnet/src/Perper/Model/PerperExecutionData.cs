using System.Threading;

namespace Perper.Model
{
    public record PerperExecutionData(
        PerperAgent Agent,
        string Delegate,
        PerperExecution Execution,
        CancellationToken CancellationToken);
}