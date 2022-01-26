using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasicSample.Streams
{
    public class Generator
    {
        public async IAsyncEnumerable<(long, string)> RunAsync(int count)
        {
            for (var i = 0 ; i < count ; i++)
            {
                var j = (i / 3 * 3) + ((i + 1) % 3); // Shuffle the order of messages to demonstrate packed functionallity

                //var randomNumber = await context.CallFunctionAsync<int, (int, int)>("GetRandomNumber", (0, 100));
                //string message = $"{i}. Message: {randomNumber}";
                var message = $"message {j}";
                Console.WriteLine($"Generating {message}");

                yield return (j, message);

                await Task.Delay(100).ConfigureAwait(false);
            }
        }
    }
}