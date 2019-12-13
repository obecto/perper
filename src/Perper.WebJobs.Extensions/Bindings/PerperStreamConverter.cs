using Apache.Ignite.Core.Binary;
using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamConverter<T> : IConverter<PerperAttribute, PerperStreamAsyncCollector<T>>
    {
        private readonly PerperFabricContext _context;
        private readonly IBinary _binary;

        public PerperStreamConverter(PerperFabricContext context, IBinary binary)
        {
            _context = context;
            _binary = binary;
        }

        public PerperStreamAsyncCollector<T> Convert(PerperAttribute attribute)
        {
            return new PerperStreamAsyncCollector<T>(_context.GetOutput(attribute.Stream), _binary);
        }
    }
}