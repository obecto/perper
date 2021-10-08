using System;
using System.Threading.Tasks;

using Perper.Extensions;

namespace BasicSample.Calls
{
    public class Init
    {
        public async Task RunAsync()
        {
            // Streams:
            const int batchCount = 10;
            const int messageCount = 28;

            var generator = await PerperContext.StreamFunction("Generator").StartAsync(messageCount).ConfigureAwait(false);
            var processor = await PerperContext.StreamFunction("Processor").StartAsync(generator, batchCount).ConfigureAwait(false);
            var _ = await PerperContext.StreamAction("Consumer").StartAsync(processor).ConfigureAwait(false);

            // Cyclic streams:
            var node1 = PerperContext.StreamFunction("Node1");
            var node2 = PerperContext.StreamAction("Node2");
            await node2.StartAsync(node1.Stream).ConfigureAwait(false);
            await node1.StartAsync(node2.Stream).ConfigureAwait(false);

            // Calls:
            var randomNumber1 = await PerperContext.CallFunctionAsync<int>("GetRandomNumber", 1, 100).ConfigureAwait(false);
            Console.WriteLine($"Random number: {randomNumber1}");

            var randomNumber2 = await PerperContext.CallFunctionAsync<int>("GetRandomNumberAsync", 1, 100).ConfigureAwait(false);
            Console.WriteLine($"Random number: {randomNumber2}");

            var (randomNumber3, randomNumber4) = await PerperContext.CallFunctionAsync<(int, int)>("GetTwoRandomNumbers", 1, 100).ConfigureAwait(false);
            Console.WriteLine($"Random numbers: {randomNumber3} + {randomNumber4}");

            var countParams = await PerperContext.CallFunctionAsync<int>("CountParams", 1, "a", "b", "c").ConfigureAwait(false);
            Console.WriteLine($"Params: {countParams}");

            await PerperContext.CallActionAsync("DoSomething", "123").ConfigureAwait(false);
            await PerperContext.CallActionAsync("DoSomethingAsync", "456").ConfigureAwait(false);

        }
    }
}