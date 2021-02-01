using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Runtime.CompilerServices;

namespace Perper.WebJobs.Extensions.Dataflow
{
    public static class DataflowEnumerableConversions
    {
        public static ISourceBlock<T> ToDataflow<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default)
        {
            var block = new BroadcastBlock<T>(null, new DataflowBlockOptions { CancellationToken = cancellationToken });

            async Task helper()
            {
                await foreach(var item in enumerable.WithCancellation(cancellationToken))
                {
                    block.Post(item);
                }
            }

            helper().ContinueWith(completedTask =>
            {
                if (completedTask.IsFaulted) ((IDataflowBlock)block).Fault(completedTask.Exception);
                else if (completedTask.IsCompletedSuccessfully) block.Complete();
            });

            return block;
        }

        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this ISourceBlock<T> block, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (!block.Completion.IsCompleted)
            {
                T item;
                try
                {
                    item = await block.ReceiveAsync(cancellationToken);
                }
                catch (InvalidOperationException)
                {
                    break;
                }
                yield return item;
            }
            await block.Completion;
        }

        public static ITargetBlock<T> ToDataflow<T>(this IAsyncCollector<T> collector, CancellationToken cancellationToken = default)
        {
            var block = new ActionBlock<T>(
                item => collector.AddAsync(item, cancellationToken),
                new ExecutionDataflowBlockOptions { CancellationToken = cancellationToken });

            return block;
        }
    }
}