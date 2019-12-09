using System;
using System.Reflection;
using Apache.Ignite.Core.Binary;

namespace Perper.Protocol
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

            UnmarshalMethod = typeof(IBinary).Assembly.GetType("Apache.Ignite.Core.Impl.Binary.Marshaller").GetMethod(
                    "Unmarshal",
                    new[]
 {
                        typeof(byte[]), typeof(IBinary).Assembly.GetType("Apache.Ignite.Core.Impl.Binary.BinaryMode")
                    })
                ?.MakeGenericMethod(typeof(IBinaryObject));

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
    }
}