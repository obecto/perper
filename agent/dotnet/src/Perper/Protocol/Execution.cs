using System.Threading;

namespace Perper.Protocol
{
    public record Execution(
        string Agent,
        string Instance,
        string Delegate,
        string ExecutionId,
        CancellationToken CancellationToken
    ) {}
}