using System;
using System.Collections.Generic;
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
        IAsyncEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query);
    }

    public interface IStream
    {
    }
}