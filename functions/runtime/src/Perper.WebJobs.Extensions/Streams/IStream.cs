using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

#if !NETSTANDARD2_0
using System.Collections.Generic;
#endif

namespace Perper.WebJobs.Extensions.Streams
{
    public interface IStream<T> : IAsyncEnumerable<T>
    {
        IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter);
        IAsyncEnumerable<T> Replay();
        IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter);
        IAsyncEnumerable<T> Replay(Func<IQueryable<T>, IQueryable<T>> query);
    }

    public interface IStream
    {
    }

#if NETSTANDARD2_0
    public interface IAsyncEnumerable<out T>
    {
        Task ForEachAsync(Action<T> action, CancellationToken cancellationToken = default);
        Task ForEachAwaitAsync(Func<T, Task> action, CancellationToken cancellationToken = default);
    }
#endif
}