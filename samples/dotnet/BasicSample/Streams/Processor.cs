using System;
using System.Collections.Generic;

using Perper.Application;
using Perper.Extensions;
using Perper.Model;

namespace BasicSample.Streams
{
    public class Processor
    {
        [PerperStreamOptions(IndexTypes = new[] { typeof(SampleUserType) })]
        public static async IAsyncEnumerable<SampleUserType> RunAsync(PerperStream generator, int batchSize)
        {
            var count = 0;
            var messagesBatch = new string[batchSize];

            await foreach (var message in generator.EnumerateAsync<string>())
            {
                if (count == batchSize)
                {
                    yield return new SampleUserType(Guid.NewGuid(), messagesBatch);
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