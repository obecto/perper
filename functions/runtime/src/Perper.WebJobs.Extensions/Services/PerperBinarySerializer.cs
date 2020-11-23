using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperBinarySerializer : IBinarySerializer
    {
        #region GetProperties
        private interface IPropertyInfo
        {
            Type Type { get; }
            object? Get(object obj);
            void Set(object obj, object? value);
        }

        private class FieldPropertyInfo : IPropertyInfo
        {
            public FieldInfo Field { get; }
            public FieldPropertyInfo(FieldInfo field)
            {
                Field = field;
            }

            public Type Type { get => Field.FieldType; }
            public object? Get(object obj) => Field.GetValue(obj);
            public void Set(object obj, object? value) => Field.SetValue(obj, value);

        }

        private class PropertyPropertyInfo : IPropertyInfo
        {
            public PropertyInfo Property { get; }
            public PropertyPropertyInfo(PropertyInfo property)
            {
                Property = property;
            }

            public Type Type { get => Property.PropertyType; }
            public object? Get(object obj) => Property.GetValue(obj);
            public void Set(object obj, object? value) => Property.SetValue(obj, value);

        }

        private readonly Dictionary<Type, Dictionary<String, IPropertyInfo>> PropertiesCache = new Dictionary<Type, Dictionary<String, IPropertyInfo>>();

        private Dictionary<String, IPropertyInfo> GetProperties(Type type)
        {
            if (PropertiesCache.TryGetValue(type, out var value))
            {
                return value;
            }

            var result = new Dictionary<String, IPropertyInfo>();

            foreach (var field in type.GetFields())
            {
                result[field.Name] = new FieldPropertyInfo(field);
            }

            // Unlike the Java version, we do not implement getters/setters here, as those are represented by properties in C#

            foreach (var property in type.GetProperties())
            {
                result[property.Name] = new PropertyPropertyInfo(property);
            }

            PropertiesCache[type] = result;

            return result;
        }
        #endregion

        #region ConvertCollections
        private readonly Dictionary<Type, Type> ConvertedTypeCache = new Dictionary<Type, Type>();

        private Type ConvertedCollectionType(Type type)
        {
            if (ConvertedTypeCache.TryGetValue(type, out var value))
            {
                return value;
            }

            if (type.IsArray)
            {
                var elementType = ConvertedCollectionType(type.GetElementType()!);
                var result = elementType.MakeArrayType();
                ConvertedTypeCache[type] = result;
                return result;
            }
            var dictionaryInterface = GetGenericInterface(type, typeof(IDictionary<,>));
            if (dictionaryInterface != null)
            {
                var keyType = ConvertedCollectionType(dictionaryInterface.GetGenericArguments()[0]);
                var valueType = ConvertedCollectionType(dictionaryInterface.GetGenericArguments()[1]);
                var result = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                ConvertedTypeCache[type] = result;
                return result;
            }
            var collectionInterface = GetGenericInterface(type, typeof(ICollection<>));
            if (collectionInterface != null)
            {
                var valueType = ConvertedCollectionType(collectionInterface.GetGenericArguments()[0]);
                var result = typeof(List<>).MakeGenericType(valueType);
                ConvertedTypeCache[type] = result;
                return result;
            }

            ConvertedTypeCache[type] = type;
            return type;
        }

        private void ConvertArray<D, S>(bool toCommon, S[] source, D[] destination)
        {
            for (var i = 0; i < source.Length; i++)
            {
                destination[i] = (D)ConvertCollections(toCommon, typeof(S), source[i])!;
            }
        }

        private void ConvertCollection<S, D>(bool toCommon, ICollection<S> source, ICollection<D> destination)
        {
            foreach (var value in source)
            {
                var convertedValue = (D)ConvertCollections(toCommon, typeof(S), value)!;
                destination.Add(convertedValue);
            }
        }

        private void ConvertDictionary<SK, SV, DK, DV>(bool toCommon, IDictionary<SK, SV> source, IDictionary<DK, DV> destination)
            where DK : notnull where SK : notnull
        {
            foreach (var (key, value) in source)
            {
                var convertedKey = (DK)ConvertCollections(toCommon, typeof(SK), key)!;
                var convertedValue = (DV)ConvertCollections(toCommon, typeof(SV), value)!;
                destination[convertedKey] = convertedValue;
            }
        }

        private object? CreateCollection(Type type)
        {
            if (type.IsInterface)
            {
                var dictionaryInterface = GetGenericInterface(type, typeof(IDictionary<,>));
                if (dictionaryInterface != null)
                {
                    return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(dictionaryInterface.GetGenericArguments()));
                }
                var collectionInterface = GetGenericInterface(type, typeof(ICollection<>));
                if (collectionInterface != null)
                {
                    return Activator.CreateInstance(typeof(List<>).MakeGenericType(collectionInterface.GetGenericArguments()));
                }
            }
            return Activator.CreateInstance(type);
        }

        private object? ConvertCollections(bool toCommon, Type type, object? source)
        {
            if (source == null)
            {
                return null;
            }

            var convertedType = ConvertedCollectionType(type);
            if (type == convertedType)
            {
                return source;
            }

            var fromType = toCommon ? type : convertedType;
            var toType = toCommon ? convertedType : type;

            if (convertedType.IsArray)
            {
                var destination = Array.CreateInstance(toType.GetElementType()!, ((Array)source!).Length);

                var method = typeof(PerperBinarySerializer).GetMethod("ConvertArray", new Type[] { typeof(bool), fromType, toType })!;
                method.Invoke(this, new object?[] { toCommon, source, destination });

                return destination;
            }
            else if (convertedType.IsGenericType)
            {
                var destination = CreateCollection(toType);

                MethodInfo method;
                if (convertedType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    method = typeof(PerperBinarySerializer).GetMethod("ConvertDictionary", new Type[] { typeof(bool), fromType, toType })!;
                }
                else
                { // if (convertedType.GetGenericTypeDefinition() == typeof(List<>))
                    method = typeof(PerperBinarySerializer).GetMethod("ConvertCollection", new Type[] { typeof(bool), fromType, toType })!;
                }
                method.Invoke(this, new object?[] { toCommon, source, destination });

                return destination;
            }

            return source; // Shouldn't get here
        }
        #endregion

        private object?[] ConvertTuple(ITuple tuple)
        {
            var result = new object?[tuple.Length];
            for (var i = 0; i < result.Length; i++)
            {
                result[i] = tuple[i];
            }
            return result;
        }

        private Type? GetGenericInterface(Type type, Type genericInterface)
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

        public void WriteBinary(object obj, IBinaryWriter writer)
        {
            if (obj is IBinarizable binarizable)
            {
                binarizable.WriteBinary(writer);
                return;
            }

            foreach (var (name, property) in GetProperties(obj.GetType()))
            {
                var value = property.Get(obj);
                var type = property.Type;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                // NOTE: nullable value types are not supported in Java/Kotlin; currently writing them as non-nullable.

                if (type == typeof(bool)) writer.WriteBoolean(name, (bool)value!);
                else if (type == typeof(char)) writer.WriteChar(name, (char)value!);
                else if (type == typeof(byte)) writer.WriteByte(name, (byte)value!);
                else if (type == typeof(short)) writer.WriteShort(name, (short)value!);
                else if (type == typeof(int)) writer.WriteInt(name, (int)value!);
                else if (type == typeof(long)) writer.WriteLong(name, (long)value!);
                else if (type == typeof(float)) writer.WriteFloat(name, (float)value!);
                else if (type == typeof(double)) writer.WriteDouble(name, (double)value!);
                else if (type == typeof(decimal)) writer.WriteDecimal(name, (decimal)value!);
                else if (type == typeof(DateTime)) writer.WriteTimestamp(name, (DateTime)value!);
                else if (type == typeof(Guid)) writer.WriteGuid(name, (Guid)value!);
                else if (type == typeof(bool[])) writer.WriteBooleanArray(name, (bool[])value!);
                else if (type == typeof(char[])) writer.WriteCharArray(name, (char[])value!);
                else if (type == typeof(byte[])) writer.WriteByteArray(name, (byte[])value!);
                else if (type == typeof(short[])) writer.WriteShortArray(name, (short[])value!);
                else if (type == typeof(int[])) writer.WriteIntArray(name, (int[])value!);
                else if (type == typeof(long[])) writer.WriteLongArray(name, (long[])value!);
                else if (type == typeof(float[])) writer.WriteFloatArray(name, (float[])value!);
                else if (type == typeof(double[])) writer.WriteDoubleArray(name, (double[])value!);
                else if (type == typeof(decimal[])) writer.WriteDecimalArray(name, (decimal?[])value!);
                else if (type == typeof(DateTime[])) writer.WriteTimestampArray(name, (DateTime?[])value!);
                else if (type == typeof(Guid[])) writer.WriteGuidArray(name, (Guid?[])value!);
                else if (type.IsEnum) writer.WriteEnum(name, value);
                else if (type.IsArray && type.GetElementType()!.IsEnum) writer.WriteEnumArray(name, (object?[])value!);
                else
                {
                    if (type.IsArray)
                    {
                        writer.WriteArray(name, (object?[])ConvertCollections(true, type, value)!);
                    }
                    else if (GetGenericInterface(type, typeof(IDictionary<,>)) != null)
                    {
                        writer.WriteDictionary(name, (Dictionary<object, object>)ConvertCollections(true, type, value)!);
                    }
                    else if (GetGenericInterface(type, typeof(ICollection<>)) != null)
                    {
                        writer.WriteCollection(name, (ICollection)ConvertCollections(true, type, value)!);
                    }
                    else if (typeof(ITuple).IsAssignableFrom(type))
                    {
                        writer.WriteArray(name, (object?[])ConvertCollections(true, typeof(object?[]), ConvertTuple((ITuple)value!))!);
                    }
                    else
                    {
                        writer.WriteObject(name, value);
                    }
                }
            }
        }

        public void ReadBinary(object obj, IBinaryReader reader)
        {
            if (obj is IBinarizable binarizable)
            {
                binarizable.ReadBinary(reader);
                return;
            }


            foreach (var (name, property) in GetProperties(obj.GetType()))
            {
                object? value;
                var type = property.Type;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                if (type == typeof(bool)) value = reader.ReadBoolean(name);
                else if (type == typeof(char)) value = reader.ReadChar(name);
                else if (type == typeof(byte)) value = reader.ReadByte(name);
                else if (type == typeof(short)) value = reader.ReadShort(name);
                else if (type == typeof(int)) value = reader.ReadInt(name);
                else if (type == typeof(long)) value = reader.ReadLong(name);
                else if (type == typeof(float)) value = reader.ReadFloat(name);
                else if (type == typeof(double)) value = reader.ReadDouble(name);
                else if (type == typeof(decimal)) value = reader.ReadDecimal(name);
                else if (type == typeof(DateTime)) value = reader.ReadTimestamp(name);
                else if (type == typeof(Guid)) value = reader.ReadGuid(name);
                else if (type == typeof(bool[])) value = reader.ReadBooleanArray(name);
                else if (type == typeof(char[])) value = reader.ReadCharArray(name);
                else if (type == typeof(byte[])) value = reader.ReadByteArray(name);
                else if (type == typeof(short[])) value = reader.ReadShortArray(name);
                else if (type == typeof(int[])) value = reader.ReadIntArray(name);
                else if (type == typeof(long[])) value = reader.ReadLongArray(name);
                else if (type == typeof(float[])) value = reader.ReadFloatArray(name);
                else if (type == typeof(double[])) value = reader.ReadDoubleArray(name);
                else if (type == typeof(decimal[])) value = reader.ReadDecimalArray(name);
                else if (type == typeof(DateTime[])) value = reader.ReadTimestampArray(name);
                else if (type == typeof(Guid[])) value = reader.ReadGuidArray(name);
                else if (type.IsEnum) value = reader.ReadEnum<object?>(name);
                else if (type.IsArray && type.GetElementType()!.IsEnum) value = reader.ReadEnumArray<object?>(name);
                else
                {
                    if (type.IsArray)
                    {
                        value = ConvertCollections(false, type, reader.ReadArray<object?>(name));
                    }
                    else if (GetGenericInterface(type, typeof(IDictionary<,>)) != null)
                    {
                        value = ConvertCollections(false, type, reader.ReadDictionary(name));
                    }
                    else if (GetGenericInterface(type, typeof(ICollection<>)) != null)
                    {
                        value = ConvertCollections(false, type, reader.ReadCollection(name));
                    }
                    else if (typeof(ITuple).IsAssignableFrom(type))
                    {
                        var values = (object?[])ConvertCollections(false, typeof(object?[]), reader.ReadArray<object?>(name))!;
                        value = Activator.CreateInstance(type, values);
                    }
                    else
                    {
                        value = reader.ReadObject<object?>(name);
                    }
                }

                property.Set(obj, value);
            }
        }
    }
}
