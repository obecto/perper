using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Perper.Protocol;

namespace Perper.Extensions
{
    public static class FabricServiceExtensions
    {
        public static Task WriteStreamItem<T>(this FabricService fabricService, string stream, T item, bool keepBinary = false)
        {
            return fabricService.WriteStreamItem(stream, FabricService.CurrentTicks, item, keepBinary);
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        public static async Task WriteExecutionResultTask(this FabricService fabricService, string call, Task<object?[]> task)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                await fabricService.WriteExecutionResult(call, result).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await fabricService.WriteExecutionException(call, exception).ConfigureAwait(false);
            }
        }

        public static Task WriteExecutionException(this FabricService fabricService, string call, Exception exception)
        {
            return fabricService.WriteExecutionError(call, exception.Message);
        }

        public static async Task<object?[]?> ReadExecutionResult(this FabricService fabricService, string call)
        {
            var (error, result) = await fabricService.ReadExecutionErrorAndResult(call).ConfigureAwait(false);

            if (error != null)
            {
                throw new InvalidOperationException($"Execution failed with error: {error}");
            }

            return result;
        }
    }
}