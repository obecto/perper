using System.Collections.Generic;

namespace BasicSample.Streams
{
    public class Processor
    {
        public static async IAsyncEnumerable<string[]> RunAsync(IAsyncEnumerable<string> generator, int batchSize)
        {
            var count = 0;
            var messagesBatch = new string[batchSize];

            await foreach (var message in generator)
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