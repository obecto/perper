using System;
using System.Threading.Tasks;
using Perper.Model;
using Perper.Extensions;
namespace MyFirstAgent
{
    public static class Init
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Hello world from Init!");
            PerperStream stream = await PerperContext
                .Stream("HelloWorldGenerator")
                .StartAsync();
            // await PerperContext.CallAsync("StreamPrinter", stream);
            PerperAgent agent = await PerperContext.StartAgentAsync("StreamPrinterAgent");
            await agent.CallAsync("StreamPrinter", stream);
        }
    }
}
