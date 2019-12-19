using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamAsyncEnumerable<T> : IAsyncEnumerable<T>
    {
        private readonly IPerperFabricContext _context;
        private readonly string _streamName;
        private readonly string _parameterName;

        private readonly IAsyncEnumerable<T> _impl;

        public PerperStreamAsyncEnumerable(IPerperFabricContext context, string streamName, string parameterName)
        {
            _context = context;
            _streamName = streamName;
            _parameterName = parameterName;

            _impl = Impl();
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return _impl.GetAsyncEnumerator(cancellationToken);
        }

        private async IAsyncEnumerable<T> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var data = _context.GetData(_streamName);
            var updates = _context.GetNotifications(_streamName).StreamParameterItemUpdates<T>(_parameterName, cancellationToken);
            await foreach (var (itemStreamName, itemKey) in updates.WithCancellation(cancellationToken))
            {
                yield return await data.FetchStreamParameterItemAsync<T>(itemStreamName, itemKey);
            }
        }
    }
}