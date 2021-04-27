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
            int count = 0;
            string[] messagesBatch = new string[parameters.batchSize];

            await foreach (string message in parameters.generator.WithCancellation(cancellationToken))
            {
                if (count == parameters.batchSize)
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
