using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperBinarySerializer : IBinarySerializer
    {
        private readonly IServiceProvider _services;

        public PerperBinarySerializer(IServiceProvider services)
        {
            _services = services;
        }

        #region GetProperties
        private interface IPropertyInfo
        {
            Type? Type { get; } // Null means that the property is not part of the final serialized form
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

            public Type? Type { get => Field.FieldType; }
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

            public Type? Type { get => Property.PropertyType; }
            public object? Get(object obj) => Property.GetValue(obj);
            public void Set(object obj, object? value) => Property.SetValue(obj, value);
        }

        private class ServicePropertyInfoDecorator : IPropertyInfo
        {
            public IPropertyInfo WrappedProperty { get; }
            public IServiceProvider ServiceProvider { get; }
            public Type ServiceType { get; }
            public ServicePropertyInfoDecorator(IPropertyInfo wrappedProperty, IServiceProvider serviceProvider, Type serviceType)
            {
                WrappedProperty = wrappedProperty;
                ServiceProvider = serviceProvider;
                ServiceType = serviceType;
            }

            public Type? Type { get => null; }
            public object? Get(object obj) => null;
            public void Set(object obj, object? value)
            {
                WrappedProperty.Set(obj, ServiceProvider.GetService(ServiceType));
            }
        }

        private readonly Dictionary<Type, Dictionary<String, IPropertyInfo>> PropertiesCache = new Dictionary<Type, Dictionary<String, IPropertyInfo>>();

        private Dictionary<String, IPropertyInfo> GetProperties(Type type)
        {
            if (PropertiesCache.TryGetValue(type, out var value))
            {
                return value;
            }

            var result = new Dictionary<String, IPropertyInfo>();

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.GetCustomAttributes<NonSerializedAttribute>().Any() ||
                    field.GetCustomAttributes<IgnoreDataMemberAttribute>().Any() ||
                    field.IsPrivate)
                {
                    continue;
                }

                result[field.Name] = new FieldPropertyInfo(field);

                if (field.GetCustomAttributes<PerperInjectAttribute>().Any())
                {
                    result[field.Name] = new ServicePropertyInfoDecorator(result[field.Name], _services, field.FieldType);
                }
            }

            // Unlike the Java version, we do not implement getters/setters here, as those are represented by properties in C#

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (property.GetCustomAttributes<IgnoreDataMemberAttribute>().Any() ||
                    !property.CanWrite)
                {
                    continue;
                }

                result[property.Name] = new PropertyPropertyInfo(property);

                if (property.GetCustomAttributes<PerperInjectAttribute>().Any())
                {
                    result[property.Name] = new ServicePropertyInfoDecorator(result[property.Name], _services, property.PropertyType);
                }
            }

            PropertiesCache[type] = result;

            return result;
        }
        #endregion

        #region ConvertCollections
        private static Func<object?, object?> Identity = x => x;

        private (Func<object?, object?> to, Func<object?, object?> from) GetCollectionConverters(Type type)
        {
            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var converters = GetCollectionConverters(elementType!);

                if (converters == (Identity, Identity))
                {
                    return (Identity, Identity);
                }

                var (converterTo, converterFrom) = converters;

                return (
                    source =>
                    {
                        if (source == null) return null;
                        var arr = (Array)source!;
                        var result = new object?[arr.Length];
                        for (var i = 0; i < arr.Length; i++)
                        {
                            result[i] = converterTo(arr.GetValue(i));
                        }
                        return result;
                    },
                    converted =>
                    {
                        if (converted == null) return null;
                        var arr = (object?[])converted!;
                        var result = Array.CreateInstance(elementType, arr.Length);
                        for (var i = 0; i < arr.Length; i++)
                        {
                            result.SetValue(converterFrom(arr[i])!, i);
                        }
                        return result;
                    });
            }

            if (typeof(ITuple).IsAssignableFrom(type))
            {
                return (
                    source =>
                    {
                        if (source == null) return null;
                        var tuple = (ITuple) source!;
                        var result = new object?[tuple.Length];
                        for (var i = 0; i < result.Length; i++)
                        {
                            result[i] = tuple[i];
                        }
                        return result;
                    },
                    converted =>
                    {
                        if (converted == null) return null;
                        return Activator.CreateInstance(type, (object?[])converted!);
                    });
            }

            var dictionaryInterface = GetGenericInterface(type, typeof(IDictionary<,>));
            if (dictionaryInterface != null)
            {
                var keyType = dictionaryInterface.GetGenericArguments()[0];
                var valueType = dictionaryInterface.GetGenericArguments()[1];

                var (keyConverterTo, keyConverterFrom) = GetCollectionConverters(keyType);
                var (valueConverterTo, valueConverterFrom) = GetCollectionConverters(valueType);

                var finalType = type.IsInterface ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : type;

                return (
                    source =>
                    {
                        if (source == null) return null;
                        var result = new Hashtable();
                        foreach (DictionaryEntry? entry in (IDictionary) source)
                        {
                            result[keyConverterTo(entry?.Key)!] = valueConverterTo(entry?.Value);
                        }
                        return result;
                    },
                    converted =>
                    {
                        if (converted == null) return null;
                        var result = (IDictionary) Activator.CreateInstance(finalType)!;
                        foreach (DictionaryEntry? entry in (IDictionary) converted)
                        {
                            result[keyConverterFrom(entry?.Key)!] = valueConverterFrom(entry?.Value);
                        }
                        return result;
                    });
            }

            var collectionInterface = GetGenericInterface(type, typeof(ICollection<>));
            if (collectionInterface != null)
            {
                var itemType = collectionInterface.GetGenericArguments()[0];

                var (itemConverterTo, itemConverterFrom) = GetCollectionConverters(itemType);

                var finalType = type.IsInterface ? typeof(List<>).MakeGenericType(itemType) : type;
                var addMethod = finalType.GetMethod(nameof(ICollection<object>.Add))!;

                return (
                    source =>
                    {
                        if (source == null) return null;
                        var result = new ArrayList();
                        foreach (object? item in (ICollection) source)
                        {
                            result.Add(itemConverterTo(item));
                        }
                        return result;
                    },
                    converted =>
                    {
                        if (converted == null) return null;
                        var result = (ICollection) Activator.CreateInstance(finalType)!;
                        foreach (object? item in (ICollection) converted)
                        {
                            addMethod.Invoke(result, new object?[] { itemConverterTo(item) });
                        }
                        return result;
                    });
            }

            return (Identity, Identity);
        }
        #endregion

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
                if (type == null) continue;
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    type = type.GetGenericArguments()[0];
                }

                // NOTE: nullable value types are not supported in Java/Kotlin; currently ignoring nullability.

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
                else if (type == typeof(string)) writer.WriteString(name, (string)value!);
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
                else if (type == typeof(string[])) writer.WriteStringArray(name, (string[])value!);
                else if (type.IsEnum) writer.WriteEnum(name, value);
                else if (type.IsArray && type.GetElementType()!.IsEnum) writer.WriteEnumArray(name, (object?[])value!);
                else
                {
                    value = GetCollectionConverters(type).to.Invoke(value);
                    if (type.IsArray || typeof(ITuple).IsAssignableFrom(type))
                    {
                        writer.WriteArray(name, (object?[])value!);
                    }
                    else if (GetGenericInterface(type, typeof(IDictionary<,>)) != null)
                    {
                        writer.WriteDictionary(name, (IDictionary)value!);
                    }
                    else if (GetGenericInterface(type, typeof(ICollection<>)) != null)
                    {
                        writer.WriteCollection(name, (ICollection)value!);
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

                if (type == null)
                {
                    property.Set(obj, null);
                    continue;
                }

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
                else if (type == typeof(string)) value = reader.ReadString(name);
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
                else if (type == typeof(string[])) value = reader.ReadStringArray(name);
                else if (type.IsEnum) value = reader.ReadEnum<object?>(name);
                else if (type.IsArray && type.GetElementType()!.IsEnum) value = reader.ReadEnumArray<object?>(name);
                else
                {
                    if (type.IsArray || typeof(ITuple).IsAssignableFrom(type))
                    {
                        value = reader.ReadArray<object?>(name);
                    }
                    else if (GetGenericInterface(type, typeof(IDictionary<,>)) != null)
                    {
                        value = reader.ReadDictionary(name);
                    }
                    else if (GetGenericInterface(type, typeof(ICollection<>)) != null)
                    {
                        value = reader.ReadCollection(name);
                    }
                    else
                    {
                        value = reader.ReadObject<object?>(name);
                    }

                    value = GetCollectionConverters(type).from.Invoke(value);
                }

                property.Set(obj, value);
            }
        }
    }
}
