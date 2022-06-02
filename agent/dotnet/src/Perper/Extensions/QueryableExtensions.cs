using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Perper.Extensions
{
    public static class QueriableExtensions
    {
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IQueryable<T> queryable)
        {
            using var enumerator = queryable.GetEnumerator();
            while (await Task.Run(enumerator.MoveNext).ConfigureAwait(false)) // Blocking, should run in background
            {
                yield return enumerator.Current;
            }
        }
    }
}