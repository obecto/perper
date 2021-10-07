using System;
using System.Threading.Tasks;

using Perper.Model;

namespace BasicSample.Calls
{
    public class Init
    {
        private readonly IContext context;

        public Init(IContext context) => this.context = context;

        public async Task RunAsync()
        {
            // Streams:
            const int batchCount = 10;
            const int messageCount = 28;

            var generator = await context.StreamFunctionAsync<string>("Generator", new object[] { messageCount }).ConfigureAwait(false);
            var processor = await context.StreamFunctionAsync<string[]>("Processor", new object[] { generator, batchCount }).ConfigureAwait(false);
            var _ = await context.StreamActionAsync("Consumer", new object[] { processor }).ConfigureAwait(false);

            // Calls:
            var randomNumber1 = await context.CallFunctionAsync<int>("GetRandomNumber", new object[] { 1, 100 }).ConfigureAwait(false);
            Console.WriteLine($"Random number: {randomNumber1}");

            var randomNumber2 = await context.CallFunctionAsync<int>("GetRandomNumberAsync", new object[] { 1, 100 }).ConfigureAwait(false);
            Console.WriteLine($"Random number: {randomNumber2}");

            var (randomNumber3, randomNumber4) = await context.CallFunctionAsync<(int, int)>("GetTwoRandomNumbers", new object[] { 1, 100 }).ConfigureAwait(false);
            Console.WriteLine($"Random numbers: {randomNumber3} + {randomNumber4}");

            await context.CallActionAsync("DoSomething", new object[] { "123" }).ConfigureAwait(false);
            await context.CallActionAsync("DoSomethingAsync", new object[] { "456" }).ConfigureAwait(false);

        }
    }
}