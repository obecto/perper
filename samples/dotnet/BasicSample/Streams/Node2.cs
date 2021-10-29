using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace BasicSample.Streams
{
    public class Node2
    {
        public async IAsyncEnumerable<int> RunAsync(PerperStream input)
        {
            await foreach (var number in input.EnumerateAsync<int>())
            {
                Console.WriteLine($"Node 2 received {number}");
                if (number > 10)
                {
                    yield break;
                }
                await Task.Delay(100).ConfigureAwait(false);
                yield return number + 2;
            }
        }
    }
}