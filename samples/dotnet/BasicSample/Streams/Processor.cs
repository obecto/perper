using System;
using System.Collections.Generic;

using Perper.Extensions;
using Perper.Model;

namespace BasicSample.Streams
{
    public class Processor
    {
        /*[PerperStreamOptions(IndexTypes = new[] { typeof(SampleUserType) })]
        public static async IAsyncEnumerable<SampleUserType> RunAsync(PerperStream generator, int batchSize)*/
        public static async IAsyncEnumerable<string> RunAsync(PerperStream generator, int batchSize)
        {
            var count = 0;
            var messagesBatch = new string[batchSize];

            await foreach (var message in generator.EnumerateAsync<string>())
            {
                if (count == batchSize)
                {
                    //yield return new SampleUserType(Guid.NewGuid(), messagesBatch);
                    yield return string.Join(':', messagesBatch);
                    count = 0;
                }

                var updatedMessage = message + "_processed";
                messagesBatch[count] = updatedMessage;
                Console.WriteLine($"Processing {message}");

                count++;
            }
        }
    }
}