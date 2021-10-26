using System;
using System.Threading;
using System.Threading.Tasks;

using Perper.Protocol;

namespace Perper.Extensions
{
    public static class AsyncLocals
    {
        private static readonly AsyncLocal<FabricService> _fabricService = new();
        private static readonly AsyncLocal<Execution> _execution = new();

        public static FabricService FabricService => _fabricService.Value!;
        public static string Agent => _execution.Value?.Agent!;
        public static string Instance => _execution.Value?.Instance!;
        public static string Delegate => _execution.Value?.Delegate!;
        public static string Execution => _execution.Value?.ExecutionId!;
        public static CancellationToken CancellationToken => _execution.Value?.CancellationToken ?? default;

        public static void SetConnection(FabricService fabricService)
        {
            _fabricService.Value = fabricService;
        }
        public static void SetExecution(Execution execution)
        {
            _execution.Value = execution;
        }
    }
}