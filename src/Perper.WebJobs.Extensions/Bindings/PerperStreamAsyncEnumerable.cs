using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly string _streamName;
        private readonly string _delegateName;
        private readonly string _parameterName;
        private readonly IPerperFabricContext _context;

        private readonly IAsyncEnumerable<T> _impl;

        public PerperStreamAsyncEnumerable(string streamName, string delegateName, string parameterName,
            IPerperFabricContext context)
        {
            _streamName = streamName;
            _delegateName = delegateName;
            _parameterName = parameterName;
            _context = context;

            _impl = Impl();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return _impl.GetAsyncEnumerator(cancellationToken);
        }

        public string GetStreamName()
        {
            return _streamName;
        }

        private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var updates = _context.GetNotifications(_delegateName).StreamParameterItemUpdates<T>(
                _streamName, _parameterName, cancellationToken);
            await foreach (var (_, item) in updates.WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}