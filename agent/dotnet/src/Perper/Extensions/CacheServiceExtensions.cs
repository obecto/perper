using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol;
using Perper.Protocol.Notifications;

namespace Perper.Extensions
{
    public static class CacheServiceExtensions
    {
        public static Task StreamAddListener(this CacheService cacheService, PerperStream stream, string callerAgent, string callerInstance, string caller, int parameter)
        {
            return cacheService.StreamAddListener(stream.Stream, callerAgent, callerInstance, caller, parameter, stream.Filter, stream.Replay, stream.LocalToData);
        }

        public static Task StreamRemoveListener(this CacheService cacheService, PerperStream stream, string caller, int parameter)
        {
            return cacheService.StreamRemoveListener(stream.Stream, caller, parameter);
        }

        public static Task<TItem> StreamReadItem<TItem>(this CacheService cacheService, StreamItemNotification notification, bool keepBinary = false)
        {
            return cacheService.StreamReadItem<TItem>(notification.Cache, notification.Key, keepBinary);
        }

        [SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "Exception is logged/handled through other means; rethrowing from handler will crash whole application.")]
        public static async Task CallWriteTask(this CacheService cacheService, string call, Task<object?[]> task)
        {
            try
            {
                var result = await task.ConfigureAwait(false);
                await cacheService.CallWriteResult(call, result).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await cacheService.CallWriteException(call, exception).ConfigureAwait(false);
            }
        }

        public static Task CallWriteException(this CacheService cacheService, string call, Exception exception)
        {
            return cacheService.CallWriteError(call, exception.Message);
        }

        public static async Task<object?[]?> CallReadResult(this CacheService cacheService, string call)
        {
            var (error, result) = await cacheService.CallReadErrorAndResult(call).ConfigureAwait(false);

            if (error != null)
            {
                throw new InvalidOperationException($"Call failed with error: {error}");
            }

            return result;
        }
    }
}