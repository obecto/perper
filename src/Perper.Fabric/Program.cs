using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
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
            var cache = ignite.GetOrCreateCache<string, IBinaryObject>("streams");
            await foreach (var streamObjects in cache.GetValuesAsync(cancellationToken))
            {
                tasks.AddRange(
                    from streamObject in streamObjects
                    select StreamBinaryTypeName.Parse(streamObject.GetBinaryType().TypeName)
                    into streamObjectTypeName

                    where streamObjectTypeName.DelegateType == DelegateType.Action
                    select new Stream(streamObjectTypeName, ignite)
                    into stream

                    select stream.Activate(cancellationToken));
            }

            await Task.WhenAll(tasks);
        }
    }
}