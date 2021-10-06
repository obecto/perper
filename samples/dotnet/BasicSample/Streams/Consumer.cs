using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasicSample.Streams
{
    public class Consumer
    {
        public async Task RunAsync(IAsyncEnumerable<string[]> input)
        {
            await foreach (var messagesBatch in input)
            {
                Console.WriteLine($"Received batch of {messagesBatch.Length} messages.\n{string.Join(", ", messagesBatch)}");
            }
        }
    }
}