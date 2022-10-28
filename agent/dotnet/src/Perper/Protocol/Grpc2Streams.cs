using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Grpc.Core;
using Grpc.Net.Client;

using Perper.Model;

using FabricStreamsClient = Perper.Protocol.Protobuf2.FabricStreams.FabricStreamsClient;

namespace Perper.Protocol
{
    public class Grpc2Streams : IPerperStreams
    {
        public Grpc2Streams(GrpcChannel grpcChannel, Grpc2TypeResolver grpc2TypeResolver, IGrpc2Caster grpc2Caster)
        {
            FabricStreamsClient = new FabricStreamsClient(grpcChannel);
            Grpc2TypeResolver = grpc2TypeResolver;
            Grpc2Caster = grpc2Caster;
        }

        private readonly FabricStreamsClient FabricStreamsClient;
        private readonly CallOptions CallOptions = new CallOptions().WithWaitForReady();
        private readonly Grpc2TypeResolver Grpc2TypeResolver;
        private readonly IGrpc2Caster Grpc2Caster;

        private static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";

        public (PerperStream Stream, Func<Task> Create) Create(PerperStreamOptions options)
        {
            var stream = new PerperStream { Stream = GenerateName(), Stride = options.Stride, StartKey = -1 };
            return (stream, CreateFunc(stream, options));
        }

        public (PerperStream Stream, Func<Task> Create) Create(PerperStreamOptions options, PerperExecution execution)
        {
            var stream = new PerperStream { Stream = execution.Execution, Stride = options.Stride, StartKey = -1 };
            return (stream, CreateFunc(stream, options));
        }

        private Func<Task> CreateFunc(PerperStream stream, PerperStreamOptions options)
        {
            return async () =>
            {
                var cacheOptions = Grpc2Caster.GetCacheOptions(stream, options);
                // cacheOptions.IndexTypeUrls.Add(options.IndexTypes.Select(Grpc2Caster.GetTypeUrl));

                await FabricStreamsClient.CreateAsync(new()
                {
                    Stream = stream,
                    CacheOptions = cacheOptions,
                    Ephemeral = !options.Persistent
                }, CallOptions);

                if (options.Action)
                {
                    await FabricStreamsClient.MoveListenerAsync(new()
                    {
                        Stream = stream,
                        ListenerName = "-trigger",
                        ReachedKey = long.MaxValue
                    }, CallOptions);
                }
            };
        }

        public async Task WriteItemAsync<TItem>(PerperStream stream, TItem item) =>
            await FabricStreamsClient.WriteItemAsync(new()
            {
                Stream = stream,
                AutoKey = true,
                Value = await Grpc2TypeResolver.SerializeAny(item).ConfigureAwait(false)
            }, CallOptions);

        public async Task WriteItemAsync<TItem>(PerperStream stream, long key, TItem item) =>
            await FabricStreamsClient.WriteItemAsync(new()
            {
                Stream = stream,
                Key = key,
                Value = await Grpc2TypeResolver.SerializeAny(item).ConfigureAwait(false)
            }, CallOptions);

        public async Task WaitForListenerAsync(PerperStream stream, CancellationToken cancellationToken) =>
            await FabricStreamsClient.WaitForListenerAsync(new()
            {
                Stream = stream
            }, CallOptions.WithCancellationToken(cancellationToken));

        public async IAsyncEnumerable<(long key, TItem value)> EnumerateItemsAsync<TItem>(PerperStream stream, string? listenerName, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            listenerName ??= GenerateName();

            await FabricStreamsClient.MoveListenerAsync(new() { Stream = stream, ListenerName = listenerName, ReachedKey = long.MinValue }, CallOptions);

            var streamItems = FabricStreamsClient.ListenItems(new()
            {
                Stream = stream.StartKey != -1 ? stream : new PerperStream(stream) { StartKey = 0 },
                ListenerName = listenerName,
                StartFromLatest = stream.StartKey == -1,
                LocalToData = stream.LocalToData
            }, CallOptions.WithCancellationToken(cancellationToken));

            await streamItems.ResponseHeadersAsync.ConfigureAwait(false);

            while (await streamItems.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false))
            {
                var item = streamItems.ResponseStream.Current;
                yield return (item.Key, (TItem)(await Grpc2TypeResolver.DeserializeAny(item.Value, typeof(TItem)).ConfigureAwait(false))!);
                await FabricStreamsClient.MoveListenerAsync(new() { Stream = stream, ListenerName = listenerName, ReachedKey = item.Key }, CallOptions);
            }
        }

        public IAsyncEnumerable<T> QuerySqlAsync<T>(PerperStream stream, string sqlCondition, params object[] sqlParameters) =>
            throw new NotImplementedException("Grpc2Streams.QuerySqlAsync is not implemented");

        public IQueryable<T> Query<T>(PerperStream stream) =>
            throw new NotImplementedException("Grpc2Streams.Query is not implemented");

        public async Task DestroyAsync(PerperStream stream) =>
            await FabricStreamsClient.DeleteAsync(new()
            {
                Stream = stream
            }, CallOptions);
    }
}