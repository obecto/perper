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

        static BinaryExtensions()
        {
            var assembly = typeof(IBinary).Assembly;
            var binary = assembly.GetType("Apache.Ignite.Core.Impl.Binary.Binary");
            var binaryMode = assembly.GetType("Apache.Ignite.Core.Impl.Binary.BinaryMode");
            var binaryObject = assembly.GetType("Apache.Ignite.Core.Impl.Binary.BinaryObject");
            var marshaller = assembly.GetType("Apache.Ignite.Core.Impl.Binary.Marshaller");

            ForceBinaryMode = Enum.Parse(binaryMode, "ForceBinary");
            MarshallerProperty = binary.GetProperty("Marshaller", BindingFlags.Instance | BindingFlags.NonPublic);
            UnmarshalMethod = marshaller.GetMethod("Unmarshal", new[] {typeof(byte[]), binaryMode})?.MakeGenericMethod(typeof(IBinaryObject));
            DataProperty = binaryObject.GetProperty("Data");
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