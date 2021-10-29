using System;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace BasicSample.Streams
{
    public class Consumer
    {
        public async Task RunAsync(PerperStream input)
        {
            await foreach (var messagesBatch in input.EnumerateAsync<string[]>())
            {
                Console.WriteLine($"Received batch of {messagesBatch.Length} messages.\n{string.Join(", ", messagesBatch)}");
            }
        }
    }
}