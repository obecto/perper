using System.Collections.Generic;

using Perper.Extensions;
using Perper.Model;

namespace BasicSample.Streams
{
    public class Processor
    {
        public static async IAsyncEnumerable<string[]> RunAsync(PerperStream generator, int batchSize)
        {
            var count = 0;
            var messagesBatch = new string[batchSize];

            await foreach (var message in generator.EnumerateAsync<string>())
            {
                if (count == batchSize)
                {
                    yield return messagesBatch;
                    count = 0;
                }

                var updatedMessage = message + "_processed";
                messagesBatch[count] = updatedMessage;

                count++;
            }
        }
    }
}