using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;

namespace Perper.WebJobs.Extensions.Bindings
{
    public class ServiceBindingProvider : IBindingProvider
    {
        private readonly IServiceProvider _services;
        private readonly ISet<Type> _types;

        public ServiceBindingProvider(ISet<Type> types, IServiceProvider services)
        {
            _types = types;
            _services = services;
        }

        public Task<IBinding?> TryCreateAsync(BindingProviderContext context)
        {
            var parameterType = context.Parameter.ParameterType;

            if (_types.Contains(parameterType) || (parameterType.IsGenericType && _types.Contains(parameterType.GetGenericTypeDefinition())))
            {
                var hasOtherBindings = context.Parameter.GetCustomAttributes(false).Any(attribute =>
                    attribute.GetType().GetCustomAttribute<BindingAttribute>() != null);

                if (!hasOtherBindings)
                {
                    return Task.FromResult<IBinding?>(new ServiceBinding(parameterType, _services));
                }
            }

            return Task.FromResult<IBinding?>(null);
        }

        private class ServiceBinding : IBinding, IValueProvider
        {
            public bool FromAttribute => false;

            public Type Type { get; }
            private readonly IServiceProvider _services;

            public ServiceBinding(Type type, IServiceProvider services)
            {
                Type = type;
                _services = services;
            }

            public Task<IValueProvider> BindAsync(BindingContext context) => Task.FromResult<IValueProvider>(this);

            public Task<IValueProvider> BindAsync(object value, ValueBindingContext context) => Task.FromResult<IValueProvider>(this);

            public Task<object> GetValueAsync() => Task.FromResult(_services.GetService(Type));

            public ParameterDescriptor ToParameterDescriptor() => new ParameterDescriptor
            {
                Name = Type.Name
            };

            public string ToInvokeString() => Type.Name;
        }
    }
}