using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Affinity;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;
using Perper.Protocol.Cache.Standard;
using Perper.Protocol.Cache.Notifications;
using Perper.Protocol.Protobuf;
using Grpc.Net.Client;
using Notification = Perper.Protocol.Cache.Notifications.Notification;
using NotificationProto = Perper.Protocol.Protobuf.Notification;

namespace Perper.Protocol.Extensions
{
    public static class PerperContextExtensions
    {
        public static Task<IBinaryObject> StreamAddListener(this PerperContext context, PerperStream stream, string caller, int parameter)
        {
            return context.StreamAddListener(stream.Stream, caller, parameter, stream.Filter, stream.Replay, stream.LocalToData);
        }

        public static Task StreamRemoveListener(this PerperContext context, PerperStream stream, string caller, int parameter)
        {
            return context.StreamRemoveListener(stream.Stream, caller, parameter);
        }

        public static Task<TItem> StreamReadItem<TItem>(this PerperContext context, StreamItemNotification notification)
        {
            return context.StreamReadItem<TItem>(notification.Cache, notification.Key);
        }

        public static async Task CallWriteTask<TResult>(this PerperContext context, string call, Task<TResult> task)
        {
            try {
                var result = await task;
                await context.CallWriteResult<TResult>(call, result);
            } catch (Exception exception) {
                await context.CallWriteException(call, exception);
            }
        }

        public static async Task CallWriteTask(this PerperContext context, string call, Task task)
        {
            try {
                await task;
                await context.CallWriteFinished(call);
            } catch (Exception exception) {
                await context.CallWriteException(call, exception);
            }
        }

        public static Task CallWriteException(this PerperContext context, string call, Exception exception)
        {
            return context.CallWriteError(call, exception.Message);
        }

        public static async Task<TResult> CallReadTask<TResult>(this PerperContext context, string call)
        {
            var (error, result) = await context.CallReadErrorAndResult<TResult>(call);

            if (error != null) {
                throw new Exception($"Call failed with error: {error}");
            }

            return result;
        }
    }
}
