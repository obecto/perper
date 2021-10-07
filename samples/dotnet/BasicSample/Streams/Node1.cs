using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Perper.Extensions;
using Perper.Model;

namespace BasicSample.Streams
{
    public class Node1
    {
        public async IAsyncEnumerable<int> RunAsync(PerperStream input)
        {
            yield return 1;
            await foreach (var number in input.EnumerateAsync<int>())
            {
                Console.WriteLine($"Node 1 received {number}");
                if (number > 10)
                {
                    yield break;
                }
                await Task.Delay(100).ConfigureAwait(false);
                yield return number - 1;
            }
        }
    }
}