using System;
using System.Linq.Expressions;

namespace Perper.WebJobs.Extensions.Model
{
    public interface IPerperStream : IAsyncDisposable
    {
        IPerperStream Subscribe();
        IPerperStream Filter<T>(Expression<Func<T, bool>> filter);
    }
}