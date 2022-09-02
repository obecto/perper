using System;

using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Client;
using Apache.Ignite.Core.Client.Cache;

using Grpc.Core;
using Grpc.Net.Client;

using Microsoft.Extensions.Options;

using Perper.Model;
using Perper.Protocol.Cache;
using Perper.Protocol.Protobuf;

namespace Perper.Protocol
{
    public sealed partial class FabricService : IPerper //, IAsyncDisposable, IDisposable
    {
        public FabricService(IIgniteClient ignite, GrpcChannel grpcChannel, IOptions<FabricConfiguration> configuration, IFabricCaster fabricCaster)
        {
            Ignite = ignite;
            FabricClient = new Fabric.FabricClient(grpcChannel);
            FabricCaster = fabricCaster;
            Configuration = configuration.Value;

            IgniteBinary = ignite.GetBinary();
            ExecutionsCache = ignite.GetOrCreateCache<string, ExecutionData>("executions");
            StreamListenersCache = ignite.GetOrCreateCache<string, StreamListener>("stream-listeners");
            // InstancesCache = ignite.GetOrCreateCache<string, InstanceData>("instances");
        }

        IPerperExecutions IPerper.Executions => this;
        IPerperAgents IPerper.Agents => this;
        IPerperStreams IPerper.Streams => this;
        IPerperStates IPerper.States => this;

        public IIgniteClient Ignite { get; }
        public IFabricCaster FabricCaster { get; }

        public FabricConfiguration Configuration { get; }

        private readonly IBinary IgniteBinary;
        private readonly ICacheClient<string, ExecutionData> ExecutionsCache;
        private readonly ICacheClient<string, StreamListener> StreamListenersCache;
        // private readonly ICacheClient<string, InstanceData> InstancesCache;

        private readonly Fabric.FabricClient FabricClient;
        private readonly CallOptions CallOptions = new CallOptions().WithWaitForReady();

        private static long CurrentTicks => DateTime.UtcNow.Ticks - DateTime.UnixEpoch.Ticks;

        private static string GenerateName(string? baseName = null) => $"{baseName}-{Guid.NewGuid()}";

        /*
         * private readonly CancellationTokenSource CancellationTokenSource = new();
        private readonly TaskCollection TaskCollection = new();

        public async ValueTask DisposeAsync()
        {
            CancellationTokenSource.Cancel();
            await TaskCollection.GetTask().ConfigureAwait(false);
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancellationTokenSource.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~FabricService() => Dispose(false);
        */
    }
}