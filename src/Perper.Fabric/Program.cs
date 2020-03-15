using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Perper.Fabric.Streams;
using Perper.Protocol.Cache;

namespace Perper.Fabric
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var ignite = Ignition.Start(new IgniteConfiguration
            {
                IgniteHome = "/usr/share/apache-ignite"
            });

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
}