using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Model;

namespace Messaging.FunctionApp
{
    public class Node
    {
        [FunctionName(nameof(Node))]
        public async Task Run([PerperStreamTrigger] PerperStreamContext context,
            [Perper("peering")] IAsyncEnumerable<Message> peering,
            [Perper("streams")] IPerperStream[] streams,
            [Perper("i")] int i,
            [Perper("n")] int n,
            [Perper("output")] IAsyncCollector<Message> output)
        {
            await Task.WhenAll(
                EnumerateInput(i, peering),
                QueryInput(i, streams.Select(stream => context.Query<Message>(stream))),
                SendOutput(i, n, output)
            );
        }

        private async Task EnumerateInput(int i, IAsyncEnumerable<Message> peering)
        {
            if (!Launcher.EnumerateMessages) return;

            await foreach (var item in peering)
            {
                Launcher.MessagesProcessed.Increment();
                if (item.To == i)
                {
                    Launcher.MessagesEnumerated.Increment();
                }
            }
        }

        private async Task QueryInput(int i, IEnumerable<IQueryable<Message>> streams)
        {
            if (!Launcher.QueryMessages) return;

            while (!Launcher.MessagesSent.IsMax())
            {
                await Task.Delay(100);
            }

            var tasks = new List<Task>();
            foreach (var stream in streams)
            {
                tasks.Add(Task.Run(() =>
                {
                    foreach (var item in stream.Where(x => x.To == i))
                    {
                        Launcher.MessagesProcessed.Increment();
                        Launcher.MessagesQueried.Increment();
                    }
                }));
            }

            await Task.WhenAll(tasks);
            await Task.Delay(1000); // Give others time to complete their queries
        }

        private async Task SendOutput(int i, int n, IAsyncCollector<Message> output)
        {
            Launcher.NodesReady.Increment();
            while (!Launcher.NodesReady.IsMax())
            {
                await Task.Delay(100);
            }

            while (true)
            {
                await output.AddAsync(new Message(i, RandomNumberGenerator.GetInt32(n)));
                Launcher.MessagesProcessed.Increment();
                if (!Launcher.MessagesSent.Increment()) break;
            }
        }
    }
}