using System;
using System.Threading.Tasks;

using Perper.Model;

namespace SimpleAgent.Calls
{
    public class Init
    {
        private readonly IContext context;

        public Init(IContext context) => this.context = context;

        public async Task RunAsync()
        {
            var container1 = await context.StartAgentAsync("simple-container-agent").ConfigureAwait(false);
            var container2 = await context.StartAgentAsync("simple-container-agent").ConfigureAwait(false);

            var id1 = await container1.CallFunctionAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);
            var id2 = await container2.CallFunctionAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);
            for (var i = 0 ; i < 127 ; i++)
            {
                if (((i ^ (i << 2)) & 8) == 0)
                {
                    var r1 = await container1.CallFunctionAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);
                    if (r1 != id1)
                    {
                        Console.WriteLine($"Fail:1 {i} {r1 == id2}");
                    }
                }
                else
                {
                    var r2 = await container2.CallFunctionAsync<Guid>("Test", new object[] { 1 }).ConfigureAwait(false);
                    if (r2 != id2)
                    {
                        Console.WriteLine($"Fail:2 {i} {r2 == id1}");
                    }
                }
            }

            // Console.WriteLine($"stream: ${await container1.CallFunctionAsync<PerperStream>("GetStream", new object[] {1}).ConfigureAwait(false)}");
            // Console.WriteLine($"stream: ${await container2.CallFunctionAsync<PerperStream>("GetStream", new object[] {2}).ConfigureAwait(false)}");
            await container1.Destroy().ConfigureAwait(false);
            await container2.Destroy().ConfigureAwait(false);

            Console.WriteLine($"Boo yaah!");

            /*
            // Streams:
            const int batchCount = 10;
            const int messageCount = 28;

            var generator = await context.StreamFunctionAsync<string>("Generator", new object[] { messageCount }).ConfigureAwait(false);
            var processor = await context.StreamFunctionAsync<string[]>("Processor", new object[] { generator, batchCount }).ConfigureAwait(false);
            var _ = await context.StreamActionAsync("Consumer", new object[] { processor }).ConfigureAwait(false);

            // Calls:
            var randomNumber = await context.CallFunctionAsync<int>("GetRandomNumber", new object[] { 1, 100 }).ConfigureAwait(false);
            Console.WriteLine($"Random number: {randomNumber}");

            var anotherRandomNumber = await context.CallFunctionAsync<int>("GetRandomNumberAsync", new object[] { 1, 100 }).ConfigureAwait(false);
            Console.WriteLine($"Random number: {anotherRandomNumber}");

            await context.CallActionAsync("DoSomething", new object[] { "123" }).ConfigureAwait(false);
            await context.CallActionAsync("DoSomethingAsync", new object[] { "456" }).ConfigureAwait(false);
            */
        }
    }
}