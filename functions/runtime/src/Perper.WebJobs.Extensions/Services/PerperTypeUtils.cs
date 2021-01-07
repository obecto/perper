using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Perper.WebJobs.Extensions.Services
{
    public static class PerperTypeUtils
    {
        public static bool IsAnonymousType(Type type)
        {
            return type.GetCustomAttributes<CompilerGeneratedAttribute>().Count() > 0;
        }
        public static bool IsTupleType(Type type)
        {
#if !NETSTANDARD2_0
            return typeof(ITuple).IsAssignableFrom(type);
#else
            var definition = type.GetGenericTypeDefinition();
            return (definition == typeof(Tuple<>)
                || definition == typeof(Tuple<,>)
                || definition == typeof(Tuple<,,>)
                || definition == typeof(Tuple<,,,>)
                || definition == typeof(Tuple<,,,,>)
                || definition == typeof(Tuple<,,,,,>)
                || definition == typeof(Tuple<,,,,,,>)
                || definition == typeof(Tuple<,,,,,,,>)
                || definition == typeof(Tuple<,,,,,,,>)
                || definition == typeof(ValueTuple<>)
                || definition == typeof(ValueTuple<,>)
                || definition == typeof(ValueTuple<,,>)
                || definition == typeof(ValueTuple<,,,>)
                || definition == typeof(ValueTuple<,,,,>)
                || definition == typeof(ValueTuple<,,,,,>)
                || definition == typeof(ValueTuple<,,,,,,>)
                || definition == typeof(ValueTuple<,,,,,,,>)
                || definition == typeof(ValueTuple<,,,,,,,>));
#endif

        }

        public static Type? GetGenericInterface(Type type, Type genericInterface)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericInterface)
            {
                return type;
            }
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == genericInterface)
                {
                    return iface;
                }
            }
            return null;
        }
    }
}