using System.Threading;

namespace Perper.Protocol
{
    public record ExecutionRecord(
        string Agent,
        string Instance,
        string Delegate,
        string Execution,
        CancellationToken CancellationToken
    ) {}
}