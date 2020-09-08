using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Messaging.FunctionApp
{
    public class Launcher
    {
        #region settings
        public static readonly int MessageCount = 100_000;
        public static readonly int NodeCount = 10;
        public static readonly bool EnumerateMessages = true;
        public static readonly bool QueryMessages = true;
        #endregion

        public static Stat MessagesSent = new Stat(0, MessageCount);
        public static Stat MessagesProcessed = new Stat();
        public static Stat MessagesEnumerated = new Stat();
        public static Stat MessagesQueried = new Stat();
        public static Stat NodesReady = new Stat(0, NodeCount);

        [FunctionName(nameof(Launcher))]
        public async Task Run([PerperModuleTrigger(RunOnStartup = true)] PerperModuleContext context,
            CancellationToken cancellationToken)
        {
            var streams = new List<IPerperStream>();

            for (var i = 0; i < NodeCount; i++)
            {
                var stream = context.DeclareStream("Node-" + i, typeof(Node), typeof(Message));
                streams.Add(stream);
            }

            var peering = await context.StreamFunctionAsync("Peering", typeof(Peering), new
            {
                streams = streams.ToArray()
            }, typeof(Message));

            for (var i = 0; i < NodeCount; i++)
            {
                await context.StreamFunctionAsync(streams[i], new
                {
                    peering = peering.Subscribe(),
                    streams = streams.ToArray(),
                    i,
                    n = NodeCount,
                });
            }

            await context.StreamActionAsync("Dummy", new
            {
                peering = peering.Subscribe()
            });

            while (!NodesReady.IsMax())
            {
                await Task.Delay(100);
            }
            var columns = "| {0,10}";
            if (EnumerateMessages)
            {
                columns += " | {1,10}";
            }
            if (QueryMessages)
            {
                columns += " | {2,10}";
            }
            columns += " | {3,10} |";
            var stats = new[] { MessagesSent.Read(), MessagesEnumerated.Read(), MessagesQueried.Read(), MessagesProcessed.Read() };
            var seconds = 0;
            Console.WriteLine("Sending {0} messages between {1} nodes", MessagesSent.Max, NodesReady.Max);
            Console.WriteLine("Per-second values:");
            Console.WriteLine(columns, "Sent", "Enumerated", "Queried", "Processed");
            Console.WriteLine(columns, "-", "-", "-", "-");
            while (true)
            {
                seconds += 1;
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(1000, cancellationToken);

                var values = stats.Select(x => (object)x.Advance()).ToArray();
                Console.WriteLine(columns, values);

                if ((long)values[3] == 0L)
                {
                    break; // Nothing processed in the last second, assume finished
                }
            }
            Console.WriteLine("Sent {0} messages between {1} nodes in roughly {2} seconds", MessagesSent.Get(), NodesReady.Get(), seconds);
        }
    }
}