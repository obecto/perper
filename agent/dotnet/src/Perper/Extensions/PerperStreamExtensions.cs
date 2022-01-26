using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Perper.Model;
using Perper.Protocol;

namespace Perper.Extensions
{
    public static class PerperStreamExtensions
    {
        public static PerperStream Replay(this PerperStream stream, bool replay = true)
        {
            return new PerperStream(stream.Stream, replay ? 0 : -1, stream.Stride, stream.LocalToData);
        }

        public static PerperStream Replay(this PerperStream stream, long replayFromKey)
        {
            return new PerperStream(stream.Stream, replayFromKey, stream.Stride, stream.LocalToData);
        }

        public static PerperStream LocalToData(this PerperStream stream, bool localToData = true)
        {
            return new PerperStream(stream.Stream, stream.StartIndex, stream.Stride, localToData);
        }

        /*public static PerperStream Filter<T>(this PerperStream stream, Expression<Func<T, bool>> filter)
        {
            return new PerperStream(stream.Stream, FilterUtils.ConvertFilter(filter), stream.StartIndex, stream.Stride, stream.LocalToData);
        }*/

        public static IQueryable<T> Query<T>(this PerperStream stream, bool keepBinary = false)
        {
            return AsyncLocals.FabricService.QueryStream<T>(stream.Stream, keepBinary);
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> queryable) // TODO: move to another class
        {
            using var enumerator = queryable.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext).ConfigureAwait(false)) // Blocking, should run in background
            {
                yield return enumerator.Current;
            }
        }

        public static async Task DestroyAsync(this PerperStream stream)
        {
            await AsyncLocals.FabricService.RemoveExecution(stream.Stream).ConfigureAwait(false); // Cancel the execution first
            await AsyncLocals.FabricService.RemoveStream(stream.Stream).ConfigureAwait(false);
        }

        public static IAsyncEnumerable<T> Query<T>(this PerperStream stream, string typeName, string sqlCondition, object[]? sqlParameters = null, bool keepBinary = false)
        {
            return AsyncLocals.FabricService.QueryStreamSql<T>(stream.Stream, $"select _VAL from {typeName.ToUpper()} {sqlCondition}", sqlParameters ?? Array.Empty<object>(), keepBinary);
        }

        public static IAsyncEnumerable<T> Query<T>(this PerperStream stream, string sqlCondition, params object[] sqlParameters)
        {
            return stream.Query<T>(typeof(T).Name, sqlCondition, sqlParameters);
        }

        public static async IAsyncEnumerable<T> EnumerateAsync<T>(
            this PerperStream stream,
            string? listenerName = null,
            bool keepBinary = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var (_, item) in stream.EnumerateWithKeysAsync<T>(listenerName, keepBinary, cancellationToken))
            {
                yield return item;
            }
        }

        public static async IAsyncEnumerable<(long, T)> EnumerateWithKeysAsync<T>(
            this PerperStream stream,
            string? listenerName = null,
            bool keepBinary = false,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var listener = listenerName == null ?
                FabricService.GenerateName(stream.Stream) :
                $"{stream.Stream}-{AsyncLocals.Execution}-{listenerName}";

            var position = listenerName == null ?
                null :
                await AsyncLocals.FabricService.GetStreamListenerPosition(listener).ConfigureAwait(false);

            var startIndex = stream.StartIndex;

            if (position == null)
            {
                await AsyncLocals.FabricService.SetStreamListenerPosition(listener, stream.Stream, FabricService.ListenerPersistAll).ConfigureAwait(false);
            }
            else
            {
                startIndex = position.Value;
            }

            var itemsEnumerable = await AsyncLocals.FabricService.EnumerateStreamItemKeys(stream.Stream, startIndex, stream.Stride, stream.LocalToData, cancellationToken).ConfigureAwait(false);

            try
            {
                await foreach (var key in itemsEnumerable)
                {
                    await AsyncLocals.FabricService.SetStreamListenerPosition(listener, stream.Stream, key).ConfigureAwait(false); // Can be optimized by updating in batches
                    T value;

                    try
                    {
                        value = await AsyncLocals.FabricService.ReadStreamItem<T>(stream.Stream, key, keepBinary).ConfigureAwait(false);
                    }
                    catch (KeyNotFoundException)
                    {
                        await Task.Delay(100, cancellationToken).ConfigureAwait(false); // Retry just in case
                        try
                        {
                            value = await AsyncLocals.FabricService.ReadStreamItem<T>(stream.Stream, key, keepBinary).ConfigureAwait(false);
                        }
                        catch (KeyNotFoundException)
                        {
                            Console.WriteLine($"Warning: Failed reading item {key} from {stream.Stream}, skipping");
                            continue;
                        }
                    }

                    yield return (key, value);
                }
            }
            finally
            {
                await AsyncLocals.FabricService.RemoveStreamListener(listener).ConfigureAwait(false);
            }
        }
    }
}