using System.Threading;

using Perper.Protocol;

namespace Perper.Extensions
{
    public static class AsyncLocals
    {
        private static readonly AsyncLocal<FabricService> _fabricService = new();
        private static readonly AsyncLocal<FabricExecution> _execution = new();

        public static FabricService FabricService => _fabricService.Value!;
        public static FabricExecution FabricExecution => _execution.Value!;
        public static string Agent => _execution.Value?.Agent!;
        public static string Instance => _execution.Value?.Instance!;
        public static string Delegate => _execution.Value?.Delegate!;
        public static string Execution => _execution.Value?.Execution!;
        public static CancellationToken CancellationToken => _execution.Value?.CancellationToken ?? default;

        public static void SetConnection(FabricService fabricService)
        {
            _fabricService.Value = fabricService;
        }
        public static void SetExecution(FabricExecution execution)
        {
            _execution.Value = execution;
        }
    }
}