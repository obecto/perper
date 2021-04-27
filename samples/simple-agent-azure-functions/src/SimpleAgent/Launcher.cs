namespace SimpleAgent
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Perper.WebJobs.Extensions.Model;
    using Perper.WebJobs.Extensions.Triggers;
    using SimpleAgent.Streams;

    /// <summary>
    /// Within the program we use 3 streams called `Generator`, `Processor` and `Consumer` for the messages processing.
    /// `Generator` stream is used to generate `messagesCount` number of messages.
    /// `Processor` stream is used to modify each message by adding suffix `_processed` and also batch messages in a collection of `batchCount` items.
    /// `Consumer` stream is used to consume the batched messages and log them.
    /// </summary>
    public class Launcher
    {
        [FunctionName(nameof(Launcher))]
        public static async Task RunAsync(
            [PerperTrigger] object input,
            IContext context,
            CancellationToken cancellationToken)
        {
            const int messagesCount = 28;
            const int batchCount = 10;

            IStream<string> generator =
                await context.StreamFunctionAsync<string>(nameof(Generator), messagesCount);

            IStream<string[]> processor =
                await context.StreamFunctionAsync<string[]>(nameof(Processor), (generator, batchCount));

            IStream consumer =
                await context.StreamActionAsync(nameof(Consumer), processor);

            var updatedMessage = await context.CallFunctionAsync<string>("UpdateMessage", "test-message");
            Console.WriteLine(updatedMessage);
        }
    }
}