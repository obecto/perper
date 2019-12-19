using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamConverter<T> : IConverter<PerperAttribute, PerperStreamAsyncCollector<T>>
    {
        private readonly IPerperFabricContext _context;

        public PerperStreamConverter(IPerperFabricContext context)
        {
            _context = context;
        }

        public PerperStreamAsyncCollector<T> Convert(PerperAttribute attribute)
        {
            return new PerperStreamAsyncCollector<T>(_context, attribute.Stream);
        }
    }
}