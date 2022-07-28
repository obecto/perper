using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Perper.Model
{
    [SuppressMessage("Style", "CA1716:Using a reserved keyword as the name of a parameter on a virtual/interface member makes it harder for consumers in other languages to override/implement the member.", Justification = "We are using 'delegate' on purpose")]
    public interface IPerperStreams
    {
        // Task<PerperStream> CreateAsync(PerperStreamOptions options, );
        #region Writer
        (PerperStream Stream, Func<Task> Create) Create(PerperStreamOptions options);
        // TODO: fully decouple streams and executions
        (PerperStream Stream, Func<Task> Create) Create(PerperStreamOptions options, PerperExecution execution);

        Task WaitForListenerAsync(PerperStream stream, CancellationToken cancellationToken = default!);
        Task WriteItemAsync<TItem>(PerperStream stream, long key, TItem item);
        Task WriteItemAsync<TItem>(PerperStream stream, TItem item);
        #endregion Writer

        #region Reader
        IAsyncEnumerable<(long key, TItem value)> EnumerateItemsAsync<TItem>(PerperStream stream, string? listenerName = null, CancellationToken cancellationToken = default!);

        IQueryable<T> Query<T>(PerperStream stream);
        IAsyncEnumerable<T> QuerySqlAsync<T>(PerperStream stream, string sqlCondition, params object[] sqlParameters);

        Task DestroyAsync(PerperStream agent);
        #endregion Reader
    }
}