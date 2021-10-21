using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol;

namespace Perper.Extensions
{
    public static class CacheServiceExtensions
    {
        public static Task WriteStreamItem<T>(this CacheService cacheService, string stream, T item, bool keepBinary = false)
        {
            return cacheService.WriteStreamItem(stream, CacheService.CurrentTicks, item, keepBinary);
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        public static async Task WriteExecutionResultTask(this CacheService cacheService, string call, Task<object?[]> task)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                await cacheService.WriteExecutionResult(call, result).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await cacheService.WriteExecutionException(call, exception).ConfigureAwait(false);
            }
        }

        public static Task WriteExecutionException(this CacheService cacheService, string call, Exception exception)
        {
            return cacheService.WriteExecutionError(call, exception.Message);
        }

        public static async Task<object?[]?> ReadExecutionResult(this CacheService cacheService, string call)
        {
            var (error, result) = await cacheService.ReadExecutionErrorAndResult(call).ConfigureAwait(false);

            if (error != null)
            {
                throw new InvalidOperationException($"Execution failed with error: {error}");
            }

            return result;
        }
    }
}