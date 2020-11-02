using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Protocol.Cache;
using Perper.WebJobs.Extensions.Services;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly string _streamName;
        private readonly string _delegateName;
        private readonly string _parameterName;
        private readonly string _parameterStreamName;
        private readonly IPerperFabricContext _context;

        private readonly IAsyncEnumerable<T> _impl;

        public PerperStreamAsyncEnumerable(string streamName, string delegateName,
            string parameterName, string parameterStreamName, IPerperFabricContext context)
        {
            _streamName = streamName;
            _delegateName = delegateName;
            _parameterName = parameterName;
            _parameterStreamName = parameterStreamName;
            _context = context;

            _impl = Impl();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return _impl.GetAsyncEnumerator(cancellationToken);
        }

        public string GetStreamName()
        {
            return _parameterStreamName;
        }

        private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // Engage input stream

            var updates = _context.GetNotifications(_delegateName).StreamParameterItemUpdates<T>(
                _streamName, _parameterName, cancellationToken);
            await foreach (var (_, item) in updates.WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }
}