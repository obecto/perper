namespace SimpleAgent.Streams
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Perper.WebJobs.Extensions.Triggers;

    public class Processor
    {
        [FunctionName(nameof(Processor))]
        public static async IAsyncEnumerable<string[]> RunAsync(
            [PerperTrigger] (IAsyncEnumerable<string> generator, int batchSize) parameters,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var count = 0;
            var messagesBatch = new string[parameters.batchSize];

            await foreach (var message in parameters.generator.WithCancellation(cancellationToken))
            {
                if (count == parameters.batchSize)
                {
                    yield return (string[])messagesBatch.Clone();
                    count = 0;
                }

                var updatedMessage = message + "_processed";
                messagesBatch[count] = updatedMessage;

                count++;
            }
        }
    }
}
