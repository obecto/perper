using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Extensions
{
    public static class PerperStreamExtensions
    {
        public static PerperStream LocalToData(this PerperStream stream, bool localToData = true)
        {
            return new PerperStream(stream.Stream, stream.Filter, stream.Replay, localToData);
        }

        public static PerperStream Filter<T>(this PerperStream stream, Expression<Func<T, bool>> filter)
        {
            return new PerperStream(stream.Stream, FilterUtils.ConvertFilter(filter), stream.Replay, stream.LocalToData);
        }

        public static PerperStream Replay(this PerperStream stream, bool replay = true)
        {
            return new PerperStream(stream.Stream, stream.Filter, replay, stream.LocalToData);
        }

        public static IQueryable<T> Query<T>(this PerperStream stream, bool keepBinary = false)
        {
            return AsyncLocals.CacheService.StreamGetQueryable<T>(stream.Stream, keepBinary);
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> queryable) // TODO: move to another class
        {
            using var enumerator = queryable.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext).ConfigureAwait(false)) // Blocking, should run in background
            {
                yield return enumerator.Current;
            }
        }

        public static IAsyncEnumerable<T> Query<T>(this PerperStream stream, string typeName, string sqlCondition, object[]? sqlParameters = null, bool keepBinary = false)
        {
            return AsyncLocals.CacheService.StreamQuerySql<T>(stream.Stream, $"select _VAL from {typeName.ToUpper()} {sqlCondition}", sqlParameters ?? Array.Empty<object>(), keepBinary);
        }

        public static IAsyncEnumerable<T> Query<T>(this PerperStream stream, string sqlCondition, params object[] sqlParameters)
        {
            return stream.Query<T>(typeof(T).Name, sqlCondition, sqlParameters);
        }

        public static async IAsyncEnumerable<T> EnumerateAsync<T>(this PerperStream stream, bool keepBinary = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var random = new Random();
            var parameter = random.Next(1, 1000000); // FIXME
            await AsyncLocals.CacheService.StreamAddListener(stream, AsyncLocals.Agent, AsyncLocals.Instance, AsyncLocals.Execution, parameter).ConfigureAwait(false);
            try
            {
                await foreach (var (key, notification) in AsyncLocals.NotificationService.GetStreamItemNotifications(AsyncLocals.Execution, parameter).ReadAllAsync(cancellationToken))
                {
                    try
                    {
                        T value;

                        try
                        {
                            value = await AsyncLocals.CacheService.StreamReadItem<T>(notification, keepBinary).ConfigureAwait(false);
                        }
                        catch (KeyNotFoundException)
                        {
                            try
                            {
                                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                                value = await AsyncLocals.CacheService.StreamReadItem<T>(notification, keepBinary).ConfigureAwait(false);
                            }
                            catch (KeyNotFoundException)
                            {
                                continue;
                            }
                        }

                        yield return value;
                    }
                    finally
                    {
                        await AsyncLocals.NotificationService.ConsumeNotification(key).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                await AsyncLocals.CacheService.StreamRemoveListener(stream, AsyncLocals.Execution, parameter).ConfigureAwait(false);
            }
        }
    }
}