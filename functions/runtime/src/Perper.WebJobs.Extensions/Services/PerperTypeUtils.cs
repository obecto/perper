using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Perper.WebJobs.Extensions.Services
{
    public static class PerperTypeUtils
    {
        private static Dictionary<Type, string> javaTypeNames = new Dictionary<Type, string>()
        {
            {typeof (bool), "java.lang.Boolean"},
            {typeof (byte), "java.lang.Byte"},
            {typeof (sbyte), "java.lang.Byte"},
            {typeof (short), "java.lang.Short"},
            {typeof (ushort), "java.lang.Short"},
            {typeof (char), "java.lang.Character"},
            {typeof (int), "java.lang.Integer"},
            {typeof (uint), "java.lang.Integer"},
            {typeof (long), "java.lang.Long"},
            {typeof (ulong), "java.lang.Long"},
            {typeof (float), "java.lang.Float"},
            {typeof (double), "java.lang.Double"},
            {typeof (string), "java.lang.String"},
            {typeof (decimal), "java.math.BigDecimal"},
            {typeof (Guid), "java.util.UUID"},
            {typeof (DateTime), "java.sql.Timestamp"},
            {typeof (Guid?), "java.util.UUID"},
            {typeof (DateTime?), "java.sql.Timestamp"}
        };

        public static bool IsPrimitiveType(Type type)
        {
            if (type.IsArray)
            {
                return IsPrimitiveType(type.GetElementType()!);
            }
            return javaTypeNames.ContainsKey(type);
        }

        public static string? GetJavaTypeName(Type type)
        {
            int arrayNesting = 0;
            while (type.IsArray)
            {
                type = type.GetElementType()!;
                arrayNesting++;
            }
            if (!javaTypeNames.TryGetValue(type, out var result))
            {
                return null;
            }
            return arrayNesting == 0 ? result : new string('[', arrayNesting) + "L" + result;
        }

        public static bool IsAnonymousType(Type type)
        {
            return type.GetCustomAttributes<CompilerGeneratedAttribute>().Count() > 0 && type.BaseType == typeof(object);
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