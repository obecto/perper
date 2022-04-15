using System;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;
namespace MyFirstAgent
{
    public static class StreamPrinter
    {
        public static async Task RunAsync(PerperStream streamToPrint) // <- Receive the passed PerperStream as a parameter
        {
            Console.WriteLine("Hello world from StreamPrinter!");
            await foreach (var ch in streamToPrint.EnumerateAsync<char>())
            {
                Console.Write(ch);
            }
        }
    }
}