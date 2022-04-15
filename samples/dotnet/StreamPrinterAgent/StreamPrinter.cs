using System;
using System.Threading.Tasks;
using Perper.Model;
using Perper.Extensions;
namespace StreamPrinterAgent
{
    public static class StreamPrinter
    {
        public static async Task RunAsync(PerperStream streamToPrint)
        {
            Console.WriteLine("Hello world from StreamPrinterAgent's StreamPrinter!");
            await foreach (var ch in streamToPrint.EnumerateAsync<char>())
            {
                Console.Write(ch);
            }
        }
    }
}
