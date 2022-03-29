using System;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace BasicSample.Calls
{
    public class Init
    {
        public async Task RunAsync()
        {
            // Streams:
            const int batchCount = 10;
            const int messageCount = 28;

            var generator = await PerperContext.Stream("Generator").Packed(1).Persistent().StartAsync(messageCount).ConfigureAwait(false);
            var processor = await PerperContext.Stream("Processor").Index<SampleUserType>().StartAsync(generator.Replay(1), batchCount).ConfigureAwait(false);
            var _ = await PerperContext.Stream("Consumer").Action().StartAsync(processor).ConfigureAwait(false);

            // Cyclic streams:
            var node1 = PerperContext.Stream("Node1");
            var node2 = PerperContext.Stream("Node2").Action();
            await node1.StartAsync(node2.Stream).ConfigureAwait(false);
            await node2.StartAsync(node1.Stream).ConfigureAwait(false);

            // Calls:
            var randomNumber1 = await PerperContext.CallAsync<int>("GetRandomNumber", 1, 100).ConfigureAwait(false);
            Console.WriteLine($"Random number: {randomNumber1}");

            var randomNumber2 = await PerperContext.CallAsync<int>("GetRandomNumberAsync", 1, 100).ConfigureAwait(false);
            Console.WriteLine($"Random number: {randomNumber2}");

            var (randomNumber3, randomNumber4) = await PerperContext.CallAsync<(int, int)>("GetTwoRandomNumbers", 1, 100).ConfigureAwait(false);
            Console.WriteLine($"Random numbers: {randomNumber3} + {randomNumber4}");

            var countParams = await PerperContext.CallAsync<int>("CountParams", 1, "a", "b", "c").ConfigureAwait(false);
            Console.WriteLine($"Params: {countParams}");

            await PerperContext.CallAsync("DoSomething", "123").ConfigureAwait(false);
            await PerperContext.CallAsync("DoSomethingAsync", "456").ConfigureAwait(false);

        }
    }
}