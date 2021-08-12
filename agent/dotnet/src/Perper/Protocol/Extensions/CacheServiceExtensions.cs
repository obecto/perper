using System;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;

using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Service;

namespace Perper.Protocol.Extensions
{
    public static class CacheServiceExtensions
    {
        public static Task<IBinaryObject> StreamAddListener(this CacheService cacheService, PerperStream stream, string callerAgent, string caller, int parameter)
        {
            return cacheService.StreamAddListener(stream.Stream, callerAgent, caller, parameter, stream.Filter, stream.Replay, stream.LocalToData);
        }

        public static Task StreamRemoveListener(this CacheService cacheService, PerperStream stream, IBinaryObject listener)
        {
            return cacheService.StreamRemoveListener(stream.Stream, listener);
        }

        public static Task StreamRemoveListener(this CacheService cacheService, PerperStream stream, string caller, int parameter)
        {
            return cacheService.StreamRemoveListener(stream.Stream, caller, parameter);
        }

        public static Task<TItem> StreamReadItem<TItem>(this CacheService cacheService, StreamItemNotification notification)
        {
            return cacheService.StreamReadItem<TItem>(notification.Cache, notification.Key);
        }

        public static async Task CallWriteTask<TResult>(this CacheService cacheService, string call, Task<TResult> task)
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

        public static async Task CallWriteTask(this CacheService cacheService, string call, Task task)
        {
            try
            {
                await task.ConfigureAwait(false);
                await cacheService.CallWriteFinished(call).ConfigureAwait(false);
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

        public static async Task<TResult> CallReadResult<TResult>(this CacheService cacheService, string call)
        {
            var (error, result) = await cacheService.CallReadErrorAndResult<TResult>(call).ConfigureAwait(false);

            if (error != null)
            {
                throw new InvalidOperationException($"Call failed with error: {error}");
            }

            return result;
        }

        public static async Task CallReadResult(this CacheService cacheService, string call)
        {
            var error = await cacheService.CallReadError(call).ConfigureAwait(false);

            if (error != null)
            {
                throw new InvalidOperationException($"Call failed with error: {error}");
            }
        }
    }
}