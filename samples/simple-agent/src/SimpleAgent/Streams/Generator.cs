namespace SimpleAgent.Streams
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Perper.WebJobs.Extensions.Triggers;

    public class Generator
    {
        [FunctionName(nameof(Generator))]
        public static async IAsyncEnumerable<string> RunAsync(
            [PerperTrigger] int count,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 0; i < count; i++)
            {
                var message = $"{i}. Message";

                yield return message;

                // Simulate slow process
                await Task.Delay(500, cancellationToken);
            }
        }
    }
}
