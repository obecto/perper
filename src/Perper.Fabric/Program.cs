using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache.Query.Continuous;
using Perper.Fabric.Streams;
using Perper.Fabric.Utils;
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
            while (!cancellationToken.IsCancellationRequested)
            {
                var completionSource = new TaskCompletionSource<IEnumerable<IBinaryObject>>();
                var listener =
                    new ActionListener<string>(events => completionSource.SetResult(events.Select(e => e.Value)));

                using (cache.QueryContinuous(new ContinuousQuery<string, IBinaryObject>(listener)))
                {
                    var streamObjects = await completionSource.Task;
                    tasks.AddRange(
                        from streamObject in streamObjects
                        select StreamBinaryTypeName.Parse(streamObject.GetBinaryType().TypeName)
                        into streamObjectTypeName

                        where streamObjectTypeName.DelegateType == DelegateType.Action
                        select new Stream(streamObjectTypeName, ignite)
                        into stream

                        select stream.Activate(cancellationToken));
                }
            }

            await Task.WhenAll(tasks);
        }
    }
}