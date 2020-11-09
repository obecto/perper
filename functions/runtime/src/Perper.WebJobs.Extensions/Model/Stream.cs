using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Client;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Model
{
    public class Stream<T> : IStream<T>
    {
        public string StreamName { get; set; }

        [NonSerialized] private readonly string? _parameterName;
        [NonSerialized] private readonly FabricService _fabric;
        [NonSerialized] private readonly IIgniteClient _ignite;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new AsyncEnumerable<T>().GetAsyncEnumerator(cancellationToken);
        }

        public IAsyncEnumerable<T> DataLocal()
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Filter(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Replay(bool dataLocal = false)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Replay(Expression<Func<T, bool>> filter, bool dataLocal = false)
        {
            throw new NotImplementedException();
        }

        public IAsyncEnumerable<T> Replay(Func<IQueryable<T>, IQueryable<T>> query, bool dataLocal = false)
        {
            throw new NotImplementedException();
        }
    }

    public class AsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        public string StreamName { get; set; }
        public Dictionary<string, object> Filter { get; set; }
        public bool LocalToData { get; set; }

        [NonSerialized] private readonly string _delegateName;
        [NonSerialized] private readonly string _parameterName;
        [NonSerialized] private readonly FabricService _fabric;
        [NonSerialized] private readonly IIgniteClient _ignite;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return Impl(cancellationToken).GetAsyncEnumerator(cancellationToken);
        }

        private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                await AddListenerAsync();

                await foreach (var notification in _fabric.GetStreamItemNotification(_delegateName, StreamName,
                    _parameterName, cancellationToken))
                {
                    yield return default!;

                    // Delete (Consume) Notification
                }
            }
            finally
            {
                await RemoveListenerAsync();
            }
        }

        private Task AddListenerAsync()
        {
            // Use _ignite to create stream listener
            // Throw exception if the stream listener already exists and it is different
            // from the one that has to be created
        }

        private Task RemoveListenerAsync()
        {
            // If listener is anonymous (_parameter is null) then remove the listener
        }
    }
}