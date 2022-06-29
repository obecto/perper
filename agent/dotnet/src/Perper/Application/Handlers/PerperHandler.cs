using System;
using System.Collections.Generic;

namespace Perper.Application.Handlers
{
    public static class PerperHandler
    {
        public static IPerperHandler? TryWrapAsyncEnumerable(IPerperHandler handler, IServiceProvider services)
        {
            foreach (var type in handler.GetType().GetInterfaces())
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IPerperHandler<>))
                {
                    var asyncEnumerableType = type.GenericTypeArguments[0];
                    if (asyncEnumerableType.IsGenericType && asyncEnumerableType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
                    {
                        var itemType = asyncEnumerableType.GenericTypeArguments[0];

                        if (itemType.IsGenericType && itemType.GetGenericTypeDefinition() == typeof(ValueTuple<,>))
                        {
                            var keyType = itemType.GenericTypeArguments[0];
                            if (keyType == typeof(long))
                            {
                                var innerItemType = itemType.GenericTypeArguments[1];

                                var keyedHandlerType = typeof(KeyedAsyncEnumerablePerperHandler<>).MakeGenericType(innerItemType);
                                return (IPerperHandler)Activator.CreateInstance(keyedHandlerType, handler, services)!;
                            }
                        }

                        var unkeyedHandlerType = typeof(AsyncEnumerablePerperHandler<>).MakeGenericType(itemType);
                        return (IPerperHandler)Activator.CreateInstance(unkeyedHandlerType, handler, services)!;
                    }
                }
            }

            return null;
        }
    }
}