using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Perper.Model;

namespace Perper.Application.Handlers
{
    public class KeyedAsyncEnumerablePerperHandler<TItem> : AsyncEnumerablePerperHandler<(long, TItem)>
    {
        public KeyedAsyncEnumerablePerperHandler(IPerperHandler<IAsyncEnumerable<(long, TItem)>> inner, IServiceProvider services) : base(inner, services)
        {
        }

        protected override async Task WriteItemsAsync(PerperStream stream, IAsyncEnumerable<(long, TItem)> items, CancellationToken cancellationToken)
        {
            await foreach (var (key, value) in items.WithCancellation(cancellationToken))
            {
                await Perper.Streams.WriteItemAsync(stream, key, value).ConfigureAwait(false);
            }
        }
    }
}