using Microsoft.Azure.WebJobs;
using Perper.WebJobs.Extensions.Config;
using Perper.WebJobs.Extensions.Services;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamConverter<T> : IConverter<PerperStreamAttribute, IAsyncCollector<T>>
    {
        private readonly IPerperFabricContext _context;

        public PerperStreamConverter(IPerperFabricContext context)
        {
            _context = context;
        }

        public IAsyncCollector<T> Convert(PerperStreamAttribute attribute)
        {
            return new PerperStreamAsyncCollector<T>(attribute.Stream, _context);
        }
    }
}