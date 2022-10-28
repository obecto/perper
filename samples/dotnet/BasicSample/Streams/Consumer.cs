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
            //await foreach (var x in input.EnumerateAsync<SampleUserType>())
            await foreach (var x in input.EnumerateAsync<string>())
            {
                //Console.WriteLine($"Received batch {x.Id} of {x.MessagesBatch.Length} messages.");
                Console.WriteLine($"Received batch {x.GetHashCode(StringComparison.InvariantCulture)} of {x.Split(':').Length} messages.");
            }
        }
    }
}