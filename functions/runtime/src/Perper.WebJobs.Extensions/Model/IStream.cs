using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Perper.WebJobs.Extensions.Model
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "<Pending>")]
    public interface IStream<T> : IAsyncEnumerable<T>, IStream
    {
        IAsyncEnumerable<T> DataLocal();
        IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false);
        IAsyncEnumerable<T> Replay(bool dataLocal = false);
        IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false);
        IAsyncEnumerable<TResult> Query<TResult>(Func<IQueryable<T>, IQueryable<TResult>> query);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "<Pending>")]
    public interface IStream
    {
    }
}