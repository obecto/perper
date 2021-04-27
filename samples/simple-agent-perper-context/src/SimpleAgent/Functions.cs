namespace SimpleAgent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Perper.WebJobs.Extensions.CustomHandler;
    using Perper.WebJobs.Extensions.Model;

    public sealed class Functions
    {
        private static PerperContext _perperContext;
        private static readonly object _padlock = new object();
        private static bool _initialized;
        private static Task _task;

        public const string GeneratorFunction = "Generator";
        public const string BatchFunction = "Batch";
        public const string ConsumerFunction = "Consumer";
        public const string UpdateMessageFunction = "UpdateMessage";

        public static Task StartAsync(PerperContext perperContext, CancellationToken cancellationToken = default)
        {
            lock (_padlock)
            {
                if (!_initialized)
                {
                    _perperContext = perperContext;
                    _task = StartFunctionsAsync(cancellationToken);
                    _initialized = true;
                }

                return _task;
            }
        }

        private static async Task StartFunctionsAsync(CancellationToken cancellationToken)
        {
            var generatorTask = GeneratorStream(cancellationToken);
            var batchTask = BatchStream(cancellationToken);
            var consumerTask = ConsumerStream(cancellationToken);
            var updateMessageTask = UpdateMessageCall(cancellationToken);

            await Task.WhenAll(generatorTask, batchTask, consumerTask, updateMessageTask);
        }

        private static async Task GeneratorStream(CancellationToken cancellationToken)
        {
            var (count, streamName) = await _perperContext.GetStreamParametersAsync<int>(GeneratorFunction, cancellationToken);

            for (var i = 0; i < count; i++)
            {
                var message = $"{i}. Message";

                await _perperContext.AddStreamOutputAsync(streamName, message);

                // Simulate slow process
                await Task.Delay(200);
            }
        }

        private static async Task BatchStream(CancellationToken cancellationToken)
        {
            var ((messages, batchSize), streamName) = await _perperContext.GetStreamParametersAsync<(IAsyncEnumerable<string>, int)>(BatchFunction, cancellationToken);

            var count = 0;
            var messagesBatch = new string[batchSize];

            await messages.ForEachAsync(async (message) =>
            {
                var updatedMessage = await _perperContext.CallFunctionAsync<string>(UpdateMessageFunction, message);
                messagesBatch[count] = updatedMessage;

                count++;

                if (count == batchSize)
                {
                    await _perperContext.AddStreamOutputAsync(streamName, (string[])messagesBatch.Clone());
                    count = 0;
                }
            }, cancellationToken);
        }

        private static async Task ConsumerStream(CancellationToken cancellationToken)
        {
            var (messages, streamName) = await _perperContext.GetStreamParametersAsync<IAsyncEnumerable<string[]>>(ConsumerFunction, cancellationToken);

            await messages.ForEachAsync((messagesBatch) =>
            {
                string output = $"Received batch of {messagesBatch.Length} messages.\n{string.Join(", ", messagesBatch)}";
                Console.WriteLine(output);
            }, cancellationToken);
        }

        private static async Task UpdateMessageCall(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var (message, token) = await _perperContext.GetCallParametersAsync<string>(UpdateMessageFunction, cancellationToken);

                var newMessage = message + "_processed";
                await _perperContext.SetCallResultAsync(token, newMessage);
            }
        }
    }
}