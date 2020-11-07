using System;
#if !NETSTANDARD2_0
using System.Collections.Generic;
#endif
using System.Linq;
using System.Linq.Expressions;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IStream<T> : IAsyncEnumerable<T>
    {
        IAsyncEnumerable<T> DataLocal();
        IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false);
        IAsyncEnumerable<T> Replay(bool dataLocal = false);
        IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false);
        IAsyncEnumerable<T> Replay(Func<IQueryable<T>, IQueryable<T>> query, bool dataLocal = false);
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