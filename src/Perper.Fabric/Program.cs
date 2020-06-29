using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Log;
using Perper.Fabric.Streams;
using Perper.Fabric.Transport;
using Perper.Protocol.Cache;
using LogLevel = Apache.Ignite.Core.Log.LogLevel;

namespace Perper.Fabric
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            SynchronizationContext.SetSynchronizationContext(
                new ThreadPoolSynchronizationContext());

            using var ignite = Ignition.Start(new IgniteConfiguration
            {
                IgniteHome = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "C:\\Ignite" : "/usr/share/apache-ignite",
            });

            await ignite.GetServices().DeployNodeSingletonAsync(nameof(TransportService), new TransportService());

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) => cancellationTokenSource.Cancel();

            var tasks = new List<Task>();
            var streams = ignite.GetOrCreateCache<string, StreamData>("streams");
            await foreach (var streamTuples in streams.QueryContinuousAsync(cancellationToken))
            {
                tasks.AddRange(
                    from streamTuple in streamTuples
                    where streamTuple.Item2.DelegateType == StreamDelegateType.Action
                    select new Stream(streamTuple.Item2, ignite).ActivateAsync(cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }

    public class ThreadPoolSynchronizationContext : SynchronizationContext
    {
    }
}