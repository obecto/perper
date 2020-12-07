using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Apache.Ignite.Core.Binary;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperBinarySerializer : IBinarySerializer
    {
        public struct NullPlaceholder { };

        private readonly IServiceProvider _services;
        private IBinary _binary = default!;

        public PerperBinarySerializer(IServiceProvider services)
        {
            _services = services;
        }

        public void SetBinary(IBinary binary)
        {
            _binary = binary;
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

        private readonly Dictionary<Type, Dictionary<string, IPropertyInfo>> PropertiesCache = new Dictionary<Type, Dictionary<string, IPropertyInfo>>();

        private Dictionary<string, IPropertyInfo> GetProperties(Type type)
        {
            if (PropertiesCache.TryGetValue(type, out var cached))
            {
                return cached;
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

        #region GetObjectConverters
        private static Func<object?, object?> Identity = x => x;

        public (Type convertedType, Func<object?, object?> to, Func<object?, object?> from) GetObjectConverters(Type type)
        {
            if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var (elementConvertedType, elementConverterTo, elementConverterFrom) = GetObjectConverters(elementType!);

                if (elementConverterTo == Identity && elementConverterFrom == Identity)
                {
                    return (type, Identity, Identity);
                }

                return (
                    typeof(object?[]),
                    source =>
                    {
                        if (source == null) return null;
                        var arr = (Array)source!;
                        var result = new object?[arr.Length];
                        for (var i = 0; i < arr.Length; i++)
                        {
                            result[i] = elementConverterTo(arr.GetValue(i));
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
                            result.SetValue(elementConverterFrom(arr[i])!, i);
                        }
                        return result;
                    }
                );
            }

#if !NETSTANDARD2_0
            if (typeof(ITuple).IsAssignableFrom(type))
            {
                var subConverters = type.GetGenericArguments().Select(x => GetObjectConverters(x)).ToList();

                return (
                    typeof(object?[]),
                    source =>
                    {
                        if (source == null) return null;
                        var tuple = (ITuple)source!;
                        var result = new object?[tuple.Length];
                        for (var i = 0; i < result.Length; i++)
                        {
                            result[i] = subConverters[i].to(tuple[i]);
                        }
                        return result;
                    },
                    converted =>
                    {
                        if (converted == null) return null;
                        var arr = (object?[])converted!;
                        var parameters = new object?[arr.Length];
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            parameters[i] = subConverters[i].from(arr[i]);
                        }
                        return Activator.CreateInstance(type, parameters);
                    }
                );
            }
#endif

            var dictionaryInterface = PerperTypeUtils.GetGenericInterface(type, typeof(IDictionary<,>));
            if (dictionaryInterface != null)
            {
                var keyType = dictionaryInterface.GetGenericArguments()[0];
                var valueType = dictionaryInterface.GetGenericArguments()[1];

                var (keyConvertedType, keyConverterTo, keyConverterFrom) = GetObjectConverters(keyType);
                var (valueConvertedType, valueConverterTo, valueConverterFrom) = GetObjectConverters(valueType);

                var finalType = type.IsInterface ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : type;

                return (
                    typeof(IDictionary),
                    source =>
                    {
                        if (source == null) return null;
                        var result = new Hashtable();
                        foreach (DictionaryEntry? entry in (IDictionary)source)
                        {
                            result[keyConverterTo(entry?.Key)!] = valueConverterTo(entry?.Value);
                        }
                        return result;
                    },
                    converted =>
                    {
                        if (converted == null) return null;
                        var result = (IDictionary)Activator.CreateInstance(finalType)!;
                        foreach (DictionaryEntry? entry in (IDictionary)converted)
                        {
                            result[keyConverterFrom(entry?.Key)!] = valueConverterFrom(entry?.Value);
                        }
                        return result;
                    }
                );
            }

            var collectionInterface = PerperTypeUtils.GetGenericInterface(type, typeof(ICollection<>));
            if (collectionInterface != null)
            {
                var itemType = collectionInterface.GetGenericArguments()[0];

                var (itemConvertedType, itemConverterTo, itemConverterFrom) = GetObjectConverters(itemType);

                var finalType = type.IsInterface ? typeof(List<>).MakeGenericType(itemType) : type;
                var addMethod = finalType.GetMethod(nameof(ICollection<object>.Add))!;

                return (
                    typeof(ICollection),
                    source =>
                    {
                        if (source == null) return null;
                        var result = new ArrayList();
                        foreach (object? item in (ICollection)source)
                        {
                            result.Add(itemConverterTo(item));
                        }
                        return result;
                    },
                    converted =>
                    {
                        if (converted == null) return null;
                        var result = (ICollection)Activator.CreateInstance(finalType)!;
                        foreach (object? item in (ICollection)converted)
                        {
                            addMethod.Invoke(result, new object?[] { itemConverterTo(item) });
                        }
                        return result;
                    }
                );
            }

            if (type.IsAssignableFrom(typeof(PerperDynamicObject)) || PerperTypeUtils.IsAnonymousType(type))
            {
                var deserializeMethod = typeof(IBinaryObject).GetMethod(nameof(IBinaryObject.Deserialize))!.MakeGenericMethod(type);

                return (
                    typeof(IBinaryObject),
                    source =>
                    {
                        if (source == null) return null;

                        if (source is PerperDynamicObject dynamicObject)
                        {
                            return dynamicObject.BinaryObject;
                        }

                        var sourceType = source.GetType();
                        if (sourceType.GetCustomAttributes<CompilerGeneratedAttribute>().Count() > 0)
                        {
                            // Compiler-generated, assume anonymous.
                            // Using Guid, as it is more likely to be unique between different programs than anonymous type name
                            var builder = _binary.GetBuilder(sourceType.GUID.ToString());

                            foreach (var property in sourceType.GetProperties())
                            {
                                builder.SetField(property.Name, property.GetValue(source));
                            }

                            return builder.Build();
                        }

                        return source;
                    },
                    converted =>
                    {
                        if (converted is IBinaryObject binObj)
                        {
                            if (type.IsAssignableFrom(typeof(PerperDynamicObject)))
                            {
                                return new PerperDynamicObject(binObj);
                            }
                            else
                            {
                                return deserializeMethod.Invoke(binObj, new object[] { });
                            }
                        }
                        return converted;
                    }
                );
            }

            return (type, Identity, Identity);
        }

        public (Type convertedType, Func<object?, object> to, Func<object, object?> from) GetRootObjectConverters(Type type)
        {
            var (convertedType, converterTo, converterFrom) = GetObjectConverters(type);
            return (
                convertedType,
                source => converterTo(source) ?? new NullPlaceholder(),
                converted =>
                {
                    if (converted is NullPlaceholder || (converted is IBinaryObject binObj && binObj.GetBinaryType().TypeName == "NullPlaceholder"))
                    {
                        return converterFrom(null);
                    }
                    return converterFrom(converted);
                }
            );
        }

        #endregion

        public void WriteBinary(object obj, IBinaryWriter writer)
        {
            if (obj is IBinarizable binarizable)
            {
                binarizable.WriteBinary(writer);
                return;
            }

            foreach (var property in GetProperties(obj.GetType()))
            {
                var name = property.Key;
                var value = property.Value.Get(obj);
                var type = property.Value.Type;
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
                    var (convertedType, converterTo, converterFrom) = GetObjectConverters(type);
                    value = converterTo.Invoke(value);

                    if (convertedType == typeof(object?[])) writer.WriteArray(name, (object?[])value!);
                    else if (convertedType == typeof(IDictionary)) writer.WriteDictionary(name, (IDictionary)value!);
                    else if (convertedType == typeof(ICollection)) writer.WriteCollection(name, (ICollection)value!);
                    else writer.WriteObject(name, value);
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

            foreach (var property in GetProperties(obj.GetType()))
            {
                var name = property.Key;
                object? value;
                var type = property.Value.Type;

                if (type == null)
                {
                    property.Value.Set(obj, null);
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
                    var (convertedType, converterTo, converterFrom) = GetObjectConverters(type);

                    if (convertedType == typeof(object?[])) value = reader.ReadArray<object?>(name);
                    else if (convertedType == typeof(IDictionary)) value = reader.ReadDictionary(name);
                    else if (convertedType == typeof(ICollection)) value = reader.ReadCollection(name);
                    else value = reader.ReadObject<object?>(name);

                    value = converterFrom.Invoke(value);
                }

                property.Value.Set(obj, value);
            }
        }
    }
}