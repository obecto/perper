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
            [Perper("enumerated")] IAsyncEnumerable<Message> enumerated,
            [Perper("filtered")] IAsyncEnumerable<Message> filtered,
            [Perper("queried")] IPerperStream[] queried,
            [Perper("i")] int i,
            [Perper("n")] int n,
            [Perper("output")] IAsyncCollector<Message> output) => await Task.WhenAll(
                ProcessEnumerated(i, enumerated),
                ProcessFiltered(i, filtered),
                ProcessQueried(i, queried.Select(stream => context.Query<Message>(stream))),
                SendOutput(i, n, output)
            );

        private async Task ProcessEnumerated(int i, IAsyncEnumerable<Message> enumerated)
        {
            await foreach (var item in enumerated)
            {
                Launcher.MessagesProcessed.Increment();
                if (item.To == i)
                {
                    Launcher.MessagesEnumerated.Increment();
                }
            }
        }

        private async Task ProcessFiltered(int i, IAsyncEnumerable<Message> filtered)
        {
            await foreach (var item in filtered)
            {
                Launcher.MessagesProcessed.Increment();
                Launcher.MessagesFiltered.Increment();
                if (item.To != i)
                {
                    System.Console.WriteLine("!!!!!");
                }
            }
        }

        private async Task ProcessQueried(int i, IEnumerable<IQueryable<Message>> queried)
        {
            while (!Launcher.MessagesSent.IsMax())
            {
                await Task.Delay(100);
            }

            var tasks = new List<Task>();
            foreach (var stream in queried)
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
                if (!Launcher.MessagesSent.Increment())
                {
                    break;
                }
            }
        }
    }
}