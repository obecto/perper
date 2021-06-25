using System.Collections.Generic;

namespace SimpleAgent.Streams
{
    public class Processor
    {
        public static async IAsyncEnumerable<string[]> RunAsync(IAsyncEnumerable<string> generator, int batchSize)
        {
            int count = 0;
            string[] messagesBatch = new string[batchSize];

            await foreach (string message in generator)
            {
                if (count == batchSize)
                {
                    yield return (string[])messagesBatch.Clone();
                    count = 0;
                }

                string updatedMessage = message + "_processed";
                messagesBatch[count] = updatedMessage;

                count++;
            }
        }
    }
}
