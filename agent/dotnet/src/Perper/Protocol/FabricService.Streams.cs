using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Client.Cache;
using Apache.Ignite.Linq;

using Grpc.Core;

using Perper.Model;
using Perper.Protocol.Cache;
using Perper.Protocol.Protobuf;

namespace Perper.Protocol
{
    public partial class FabricService : IPerperStreams
    {
        public const long ListenerPersistAll = long.MinValue;
        public const long ListenerJustTrigger = long.MaxValue;

        (PerperStream Stream, Func<Task> Create) IPerperStreams.Create(PerperStreamOptions options)
        {
            var stream = new PerperStream(GenerateName(""), -1, options.Stride, false);
            var indexes = options.IndexTypes.SelectMany(FabricCaster.TypeToQueryEntities).ToArray();
            return (stream, async () =>
            {
                await Task.Run(() => Ignite.CreateCache<long, object>(new CacheClientConfiguration(stream.Stream, indexes))).ConfigureAwait(false);
            }
            );
        }

        (PerperStream Stream, Func<Task> Create) IPerperStreams.Create(PerperStreamOptions options, PerperExecution execution)
        {
            var stream = new PerperStream(execution.Execution, -1, options.Stride, false);
            var indexes = options.IndexTypes.SelectMany(FabricCaster.TypeToQueryEntities).ToArray();
            return (stream, async () =>
            {
                await Task.Run(() => Ignite.CreateCache<long, object>(new CacheClientConfiguration(stream.Stream, indexes))).ConfigureAwait(false);

                if (options.Persistent)
                {
                    await StreamListenersCache.PutAsync($"-{stream.Stream}-persist", new StreamListener(stream.Stream, ListenerPersistAll)).ConfigureAwait(false);
                }
                else if (options.Action)
                {
                    await StreamListenersCache.PutAsync($"-{stream.Stream}-trigger", new StreamListener(stream.Stream, ListenerJustTrigger)).ConfigureAwait(false);
                }
            }
            );
        }

        async Task IPerperStreams.WaitForListenerAsync(PerperStream stream, CancellationToken cancellationToken)
        {
            await FabricClient.ListenerAttachedAsync(new ListenerAttachedRequest
            {
                Stream = stream.Stream
            }, CallOptions.WithCancellationToken(cancellationToken));
        }

        Task IPerperStreams.WriteItemAsync<TItem>(PerperStream stream, TItem item) =>
            ((IPerperStreams)this).WriteItemAsync<TItem>(stream, CurrentTicks, item);

        async Task IPerperStreams.WriteItemAsync<TItem>(PerperStream stream, long key, TItem item)
        {
            await GetItemCache<TItem>(stream).PutIfAbsentOrThrowAsync(key, FabricCaster.PackItem(item)).ConfigureAwait(false);
        }

        async IAsyncEnumerable<(long, TItem)> IPerperStreams.EnumerateItemsAsync<TItem>(PerperStream stream, string? listenerName, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // Ideally should include e.g. the execution in the listener name
            var listener = listenerName == null ? GenerateName($"{stream.Stream}") : $"{stream.Stream}-{listenerName}";

            var existing = await StreamListenersCache.GetAndPutIfAbsentAsync(listener, new StreamListener(stream.Stream, ListenerPersistAll)).ConfigureAwait(false);
            if (existing.Success)
            {
                stream = new PerperStream(stream.Stream, existing.Value.Position, stream.Stride, stream.LocalToData); // stream.Replay();
            }

            try
            {
                await foreach (var (key, value) in EnumerateItemsWithoutListenerAsync<TItem>(stream, cancellationToken))
                {
                    await StreamListenersCache.PutAsync(listener, new StreamListener(stream.Stream, key)).ConfigureAwait(false); // Can be optimized by updating in batches

                    yield return (key, value);
                }
            }
            finally
            {
                await StreamListenersCache.RemoveAsync(listener).ConfigureAwait(false);
            }
        }

        public async IAsyncEnumerable<(long, TItem)> EnumerateItemsWithoutListenerAsync<TItem>(
            PerperStream stream,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var itemsCache = GetItemCache<TItem>(stream);

            await foreach (var key in EnumerateKeysWithoutListenerAsync(stream, cancellationToken))
            {
                var result = await itemsCache.TryGetAsync(key).ConfigureAwait(false);

                if (!result.Success) // Retry, just in case there was some race condition
                {
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                    result = await itemsCache.TryGetAsync(key).ConfigureAwait(false);
                }

                if (!result.Success)
                {
                    Console.WriteLine($"Warning: Failed reading item {key} from {stream.Stream}, skipping");
                    continue;
                }

                yield return (key, FabricCaster.UnpackItem<TItem>(result.Value));
            }
        }

        private async IAsyncEnumerable<long> EnumerateKeysWithoutListenerAsync(PerperStream stream, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var streamItems = FabricClient.StreamItems(new StreamItemsRequest
            {
                Stream = stream.Stream,
                StartKey = stream.StartIndex,
                Stride = stream.Stride,
                LocalToData = stream.LocalToData
            }, CallOptions.WithCancellationToken(cancellationToken));

            await streamItems.ResponseHeadersAsync.ConfigureAwait(false);

            while (await streamItems.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var item = streamItems.ResponseStream.Current;
                yield return item.Key;
            }
        }

        // TODO: Try to come up with a way for .Query to pass through FabricCaster.UnpackItem
        IQueryable<TItem> IPerperStreams.Query<TItem>(PerperStream stream)
        {
            if (FabricCaster.TypeShouldKeepBinary(typeof(TItem)))
            {
                throw new InvalidOperationException("Cannot create IQueryable<{typeof(TItem)}> for a type that needs to be converted to binary");
            }
            return Ignite.GetCache<long, TItem>(stream.Stream).AsCacheQueryable().Select(pair => pair.Value);
        }

        IAsyncEnumerable<TItem> IPerperStreams.QuerySqlAsync<TItem>(PerperStream stream, string sqlCondition, params object[] sqlParameters)
        {
            return QuerySqlAsync<TItem>(stream, sqlCondition, sqlParameters);
        }

        public async IAsyncEnumerable<TItem> QuerySqlAsync<TItem>(PerperStream stream, string sqlCondition, object[] sqlParameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var queryEntity = FabricCaster.TypeToQueryEntities(typeof(TItem)).Single();
            var sql = $"select {queryEntity.ValueFieldName} from {queryEntity.TableName} {sqlCondition}";

            var query = new SqlFieldsQuery(sql, false, sqlParameters);

            using var cursor = GetItemCache<TItem>(stream).Query(query);
            using var enumerator = cursor.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext, cancellationToken).ConfigureAwait(false)) // Blocking, should run in background
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return FabricCaster.UnpackItem<TItem>(enumerator.Current[0]);
            }
        }

        async Task IPerperStreams.DestroyAsync(PerperStream stream)
        {
            await ((IPerperExecutions)this).DestroyAsync(new PerperExecution(stream.Stream)).ConfigureAwait(false);
            await Task.Run(() => Ignite.DestroyCache(stream.Stream)).ConfigureAwait(false);
        }

        private ICacheClient<long, object> GetItemCache<TItem>(PerperStream stream)
        {
            return Ignite.GetCache<long, object>(stream.Stream).WithKeepBinary(FabricCaster.TypeShouldKeepBinary(typeof(TItem)));
        }
    }
}