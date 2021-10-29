using System.Threading;

namespace Perper.Protocol
{
    public record FabricExecution(
        string Agent,
        string Instance,
        string Delegate,
        string Execution,
        CancellationToken CancellationToken
    )
    { }
}