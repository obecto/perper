using System;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;
namespace MyFirstAgent
{
    public static class Deploy
    {
        public static async Task RunAsync()
        {
            Console.WriteLine("Hello world from Deploy!");
            var stream = await PerperContext.CallAsync<PerperStream>("HelloWorldGenerator");
            //await PerperContext.CallAsync("StreamPrinter", stream);
            var agent = await PerperContext.StartAgentAsync("StreamPrinterAgent");
            await agent.CallAsync("StreamPrinter", stream);
        }
    }
}