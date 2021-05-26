using System;

using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

using Perper.WebJobs.Extensions.Bindings;

namespace Perper.WebJobs.Extensions.Config
{
    public class PerperCollectorConverter<T> : IConverter<PerperAttribute, IAsyncCollector<T>>
    {
        private readonly IServiceProvider _services;

        public PerperCollectorConverter(IServiceProvider services) => _services = services;

        public IAsyncCollector<T> Convert(PerperAttribute input) => (IAsyncCollector<T>)ActivatorUtilities.CreateInstance(_services, typeof(PerperCollector<T>), input.Stream);
    }
}