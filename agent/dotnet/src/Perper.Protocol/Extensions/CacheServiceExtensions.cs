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
                var result = await task;
                await cacheService.CallWriteResult<TResult>(call, result);
            }
            catch (Exception exception)
            {
                await cacheService.CallWriteException(call, exception);
            }
        }

        public static async Task CallWriteTask(this CacheService cacheService, string call, Task task)
        {
            try
            {
                await task;
                await cacheService.CallWriteFinished(call);
            }
            catch (Exception exception)
            {
                await cacheService.CallWriteException(call, exception);
            }
        }

        public static Task CallWriteException(this CacheService cacheService, string call, Exception exception)
        {
            return cacheService.CallWriteError(call, exception.Message);
        }

        public static async Task<TResult> CallReadResult<TResult>(this CacheService cacheService, string call)
        {
            var (error, result) = await cacheService.CallReadErrorAndResult<TResult>(call);

            if (error != null)
            {
                throw new Exception($"Call failed with error: {error}");
            }

            return result;
        }

        public static async Task CallReadResult(this CacheService cacheService, string call)
        {
            var error = await cacheService.CallReadError(call);

            if (error != null)
            {
                throw new Exception($"Call failed with error: {error}");
            }
        }
    }
}