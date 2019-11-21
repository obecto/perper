using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Apache.Ignite.Core.Binary;

namespace Ignite.Extensions
{
    public static class BinaryExtensions
    {
        private static readonly object ForceBinaryMode;
        private static readonly PropertyInfo MarshallerProperty;
        private static readonly MethodInfo UnmarshalMethod;
        private static readonly PropertyInfo DataProperty;

        private const string CacheObjectTypeNamePrefix = "Ignite.Extensions.Cache.";

        static BinaryExtensions()
        {
            ForceBinaryMode =
                Enum.Parse(typeof(IBinary).Assembly.GetType("Apache.Ignite.Core.Impl.Binary.BinaryMode"),
                    "ForceBinary");
            MarshallerProperty =
                typeof(IBinary).Assembly.GetType("Apache.Ignite.Core.Impl.Binary.Binary")
                    .GetProperty("Marshaller", BindingFlags.Instance | BindingFlags.NonPublic);
            
            // ReSharper disable once PossibleNullReferenceException
            UnmarshalMethod = typeof(IBinary).Assembly.GetType("Apache.Ignite.Core.Impl.Binary.Marshaller").GetMethod(
                "Unmarshal",
                new Type[]
                {
                    typeof(byte[]), typeof(IBinary).Assembly.GetType("Apache.Ignite.Core.Impl.Binary.BinaryMode")
                }).MakeGenericMethod(typeof(IBinaryObject));
            
            DataProperty = typeof(IBinaryObject).Assembly.GetType("Apache.Ignite.Core.Impl.Binary.BinaryObject")
                .GetProperty("Data");
        }

        public static IBinaryObject GetBinaryObjectFromBytes(this IBinary binary, byte[] value)
        {
            //TODO: message size delimited and change to return type to IEnumerable
            //TODO: handle incomplete bytes chunks

            return (IBinaryObject) UnmarshalMethod.Invoke(MarshallerProperty.GetValue(binary),
                new[] {value, ForceBinaryMode});
        }

        public static byte[] GetBytesFromBinaryObject(this IBinary binary, IBinaryObject value)
        {
            //TODO: message size delimited and change the input type to IEnumerable
            return (byte[]) DataProperty.GetValue(value);
        }

        public static IBinaryObjectBuilder GetCacheObjectBuilder(this IBinary binary, string cacheName)
        {
            return binary.GetBuilder($"{CacheObjectTypeNamePrefix}{cacheName}");
        }

        public static IBinaryObjectBuilder GetCacheObjectBuilder(this IBinary binary, string cacheName,
            Type cacheType)
        {
            return binary.GetBuilder($"{CacheObjectTypeNamePrefix}{cacheName}<{cacheType}>");
        }

        public static Tuple<string, string> ParseCacheObjectTypeName(this IBinaryObject binaryObject)
        {
            var regex = new Regex($@"{CacheObjectTypeNamePrefix}(.*)(<.*>)?");
            var match = regex.Match(binaryObject.GetBinaryType().TypeName);
            var cacheName = match.Groups[0].Value;
            var cacheType = match.Groups.Count > 1 ? match.Groups[1].Value : null;
            return Tuple.Create(cacheName, cacheType);
        }

        public static IEnumerable<Tuple<string, string, string>> SelectReferencedCacheObjects(
            this IBinaryObject binaryObject)
        {
            var cacheObjectReference =
                new Func<string, Tuple<string, string, string>>(cacheParam =>
                {
                    var (cacheName, cacheType) =
                        binaryObject.GetField<IBinaryObject>(cacheParam).ParseCacheObjectTypeName();
                    return Tuple.Create(cacheParam, cacheName, cacheType);
                });

            return
                from field in binaryObject.GetBinaryType().Fields
                where binaryObject.GetBinaryType().GetFieldTypeName(field).StartsWith(CacheObjectTypeNamePrefix)
                select cacheObjectReference(field);
        }
    }
}