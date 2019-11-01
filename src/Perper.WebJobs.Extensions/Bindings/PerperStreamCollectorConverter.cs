using Microsoft.Azure.WebJobs;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class PerperStreamCollectorConverter<T> : IConverter<PerperStreamAttribute, IAsyncCollector<T>>
    {
        public IAsyncCollector<T> Convert(PerperStreamAttribute input)
        {
            
        }
    }
}