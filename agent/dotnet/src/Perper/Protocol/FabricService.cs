using System;
using System.Threading;
using System.Threading.Tasks;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Grpc.Core;
using Grpc.Net.Client;

using Perper.Model;
using Perper.Protocol.Cache;
using Perper.Protocol.Protobuf;

namespace Perper.Protocol
{
    public sealed partial class FabricService : IAsyncDisposable, IDisposable, IPerper
    {
        public FabricService(IIgniteClient ignite, GrpcChannel grpcChannel, IFabricCaster fabricCaster)
        {
            Ignite = ignite;
            _fabricClient = new Fabric.FabricClient(grpcChannel);
            FabricCaster = fabricCaster;

            _igniteBinary = ignite.GetBinary();
            _executionsCache = ignite.GetOrCreateCache<string, ExecutionData>("executions");
            _streamListenersCache = ignite.GetOrCreateCache<string, StreamListener>("stream-listeners");
            _instancesCache = ignite.GetOrCreateCache<string, InstanceData>("instances");
        }

        IPerperExecutions IPerper.Executions => this;
        IPerperAgents IPerper.Agents => this;
        IPerperStreams IPerper.Streams => this;
        IPerperStates IPerper.States => this;

        public IIgniteClient Ignite { get; }
        public IFabricCaster FabricCaster { get; }

        private readonly IBinary _igniteBinary;
        private readonly ICacheClient<string, ExecutionData> _executionsCache;
        private readonly ICacheClient<string, StreamListener> _streamListenersCache;
        private readonly ICacheClient<string, InstanceData> _instancesCache;

        private readonly Fabric.FabricClient _fabricClient;
        private CallOptions _callOptions = new CallOptions().WithWaitForReady();

        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly TaskCollection _taskCollection = new();

        private static long CurrentTicks => DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;

        private static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";

        public async ValueTask DisposeAsync()
        {
            _cancellationTokenSource.Cancel();
            await _taskCollection.GetTask().ConfigureAwait(false);
            Dispose(true);
#pragma warning disable CA1816
            GC.SuppressFinalize(this);
#pragma warning restore CA1816
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FabricService() => Dispose(false);
    }
}