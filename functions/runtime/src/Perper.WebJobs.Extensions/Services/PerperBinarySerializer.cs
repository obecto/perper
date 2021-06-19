using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.Serialization;
using Apache.Ignite.Core.Binary;
using Perper.WebJobs.Extensions.Model;

namespace Perper.WebJobs.Extensions.Services
{
    public class PerperBinarySerializer : IBinarySerializer
    {
        [PerperData(Name = "<null>")]
        public struct NullPlaceholder { };

        private readonly IServiceProvider? _services;
        private IBinary? _binary = null;

        public PerperBinarySerializer(IServiceProvider? services)
        {
            _services = services;
        }

        public void SetBinary(IBinary binary)
        {
            _binary = binary;
        }

        #region GetProperties
        private class TypeData
        {
            public ConstructorInfo? Constructor { get; set; }
            public List<FieldOrPropertyInfo> Properties { get; } = new List<FieldOrPropertyInfo>();
        }
        private struct FieldOrPropertyInfo
        {
            public MemberInfo Member { get; }
            public FieldOrPropertyInfo(FieldInfo field)
            {
                Member = field;
            }
            public FieldOrPropertyInfo(PropertyInfo property)
            {
                Member = property;
            }

            public string Name => Member.Name;
#pragma warning disable CS8509 // Switch handles all possibilities
            public Type Type => Member switch { FieldInfo fi => fi.FieldType, PropertyInfo pi => pi.PropertyType };
            public object? GetValue(object obj) => Member switch { FieldInfo fi => fi.GetValue(obj), PropertyInfo pi => pi.GetValue(obj) };
            public void SetValue(object obj, object? value)
            {
                switch (Member)
                {
                    case FieldInfo fi: fi.SetValue(obj, value); break;
                    case PropertyInfo pi: pi.SetValue(obj, value); break;
                }
            }
#pragma warning restore CS8509
        }

        private readonly ConcurrentDictionary<Type, TypeData> TypeDataCache = new ConcurrentDictionary<Type, TypeData>();

        private TypeData GetTypeData(Type type) => TypeDataCache.GetOrAdd(type, _ =>
        {
            var allInstanceBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            var result = new TypeData();

            foreach (var field in type.GetFields(allInstanceBindingFlags))
            {
                if (field.GetCustomAttributes<NonSerializedAttribute>().Any() ||
                    field.GetCustomAttributes<IgnoreDataMemberAttribute>().Any() ||
                    field.IsPrivate)
                {
                    continue;
                }

                result.Properties.Add(new FieldOrPropertyInfo(field));
            }

            // Unlike the Java version, we do not implement getters/setters here, as those are represented by properties in C#

            foreach (var property in type.GetProperties(allInstanceBindingFlags))
            {
                if (property.GetCustomAttributes<IgnoreDataMemberAttribute>().Any() ||
                    property.GetIndexParameters().Length != 0 ||
                    !property.CanRead)
                {
                    continue;
                }

                result.Properties.Add(new FieldOrPropertyInfo(property));
            }

            result.Properties.Sort((x, y) => x.Name.CompareTo(y.Name));

            result.Constructor = type.GetConstructors(allInstanceBindingFlags).SingleOrDefault(c => c.GetCustomAttribute<PerperInjectAttribute>() != null);

            return result;
        });
        #endregion

        #region ObjectConverters
        public object? Serialize(object? value)
        {
            switch (value)
            {
                case null: return null;
                case var primitive when PerperTypeUtils.IsPrimitiveType(value.GetType()): return primitive;

                case Array arr:
                    {
                        var serialized = new object?[arr.Length];
                        for (var i = 0; i < arr.Length; i++)
                        {
                            serialized[i] = Serialize(arr.GetValue(i));
                        }
                        return serialized;
                    }
                case var tuple when PerperTypeUtils.IsTupleType(tuple.GetType()):
                    {
                        // Cannot use ITuple cast since it doesn't work in netstandard2
                        var typeData = GetTypeData(tuple.GetType());
                        var serialized = new object?[typeData.Properties.Count];
                        for (var i = 0; i < typeData.Properties.Count; i++)
                        {
                            var tupleElement = typeData.Properties[i].GetValue(tuple);
                            serialized[i] = Serialize(tupleElement);
                        }
                        return serialized;
                    }
                case IDictionary dictionary:
                    {
                        var serialized = new Hashtable();
                        foreach (DictionaryEntry? entry in dictionary)
                        {
                            serialized[Serialize(entry?.Key)!] = Serialize(entry?.Value);
                        }
                        return serialized;
                    }
                case ICollection collection:
                    {
                        var serialized = new ArrayList();
                        foreach (object? item in collection)
                        {
                            serialized.Add(Serialize(item));
                        }
                        return serialized;
                    }
                case var anonymous when PerperTypeUtils.IsAnonymousType(value.GetType()):
                    {
                        var anonymousType = anonymous.GetType();

                        if (_binary != null)
                        {
                            var builder = _binary.GetBuilder(anonymousType.GUID.ToString());

                            foreach (var property in anonymousType.GetProperties())
                            {
                                builder.SetField(property.Name, property.GetValue(anonymous));
                            }

                            return builder.Build();
                        }
                        else
                        {
                            // We are used in testing; return an object which works with dynamic
                            var expando = new ExpandoObject();

                            foreach (var property in anonymousType.GetProperties())
                            {
                                (expando as IDictionary<string, object?>).Add(property.Name, property.GetValue(anonymous));
                            }

                            return expando;
                        }

                    }

                case PerperDynamicObject dynamicObject: return dynamicObject.BinaryObject;

                case BigInteger bigInteger: return bigInteger.ToString();

                default: return value;
            }
        }

        public object? Deserialize(object? serialized, Type type)
        {
            switch (serialized)
            {
                case null: return null;

                case var primitive when PerperTypeUtils.IsPrimitiveType(serialized.GetType()) && serialized.GetType() == type: return primitive;

                case Array arr when type.IsArray:
                    {
                        var elementType = type.GetElementType()!;
                        var value = Array.CreateInstance(elementType, arr.Length);
                        for (var i = 0; i < arr.Length; i++)
                        {
                            value.SetValue(Deserialize(arr.GetValue(i), elementType), i);
                        }
                        return value;
                    }

                case Array arr when PerperTypeUtils.IsTupleType(type):
                    {
                        // Can potentitially rework this to use TypeData, similar to the code serializing tuples
                        var types = type.GetGenericArguments();
                        var parameters = new object?[types.Length];
                        for (var i = 0; i < types.Length && i < arr.Length; i++)
                        {
                            parameters[i] = Deserialize(arr.GetValue(i), types[i]);
                        }
                        return Activator.CreateInstance(type, parameters);
                    }

                case Array arr: return arr;

                case IDictionary dictionary:
                    {
                        // NOTE: Can cache keyType, valueType, and finalType for type
                        var dictionaryTypes = PerperTypeUtils.GetGenericInterface(type, typeof(IDictionary<,>))?.GetGenericArguments() ?? new[] { typeof(object), typeof(object) };
                        var keyType = dictionaryTypes[0];
                        var valueType = dictionaryTypes[1];
                        var finalType = type.IsInterface ? typeof(Dictionary<,>).MakeGenericType(keyType, valueType) : type;

                        var value = (IDictionary)Activator.CreateInstance(finalType)!;
                        foreach (DictionaryEntry? entry in dictionary)
                        {
                            value[Deserialize(entry?.Key, keyType)!] = Deserialize(entry?.Value, valueType);
                        }
                        return value;
                    }

                case ICollection collection:
                    {
                        // NOTE: Can cache elementType, finalType, and addMethod for type
                        var elementType = PerperTypeUtils.GetGenericInterface(type, typeof(ICollection<>))?.GetGenericArguments()?[0] ?? typeof(object);
                        var finalType = type.IsAssignableFrom(typeof(List<>).MakeGenericType(elementType)) ? typeof(List<>).MakeGenericType(elementType) : type;

                        var addMethod = finalType.GetMethod(nameof(ICollection<object>.Add))!;

                        var value = (ICollection)Activator.CreateInstance(finalType)!;
                        foreach (object? item in collection)
                        {
                            addMethod.Invoke(value, new object?[] { Deserialize(item, elementType) });
                        }
                        return value;
                    }

                case string stringValue when type == typeof(BigInteger):
                    return BigInteger.Parse(stringValue);

                case IBinaryObject binaryObject:
                    {
                        if (type == typeof(PerperDynamicObject) || Guid.TryParse(binaryObject.GetBinaryType().TypeName, out var typeGuid))
                        {
                            return new PerperDynamicObject(binaryObject);
                        }
                        else
                        {
                            // NOTE: Can cache deserializeMethod for type
                            var deserializeMethod = typeof(IBinaryObject).GetMethod(nameof(IBinaryObject.Deserialize))!.MakeGenericMethod(type);
                            return deserializeMethod.Invoke(binaryObject, new object[] { });
                        }
                    }

                default: return serialized;
            }
        }

        public object SerializeRoot(object? value)
        {
            return Serialize(value) ?? new NullPlaceholder();
        }

        public object? DeserializeRoot(object value, Type type)
        {
            if (value is NullPlaceholder || (value is IBinaryObject binaryObject && binaryObject.GetBinaryType().TypeName == "<null>"))
            {
                return Deserialize(null, type);
            }
            return Deserialize(value, type);
        }

        public Dictionary<string, string> GetQueriableFields(Type type)
        {
            var result = new Dictionary<string, string>();
            foreach (var property in GetTypeData(type).Properties)
            {
                var name = property.Name;
                var typeName = PerperTypeUtils.GetJavaTypeName(property.Type);
                if (typeName != null)
                {
                    result[name] = typeName;
                }
            }
            return result;
        }
        #endregion

        public void WriteBinary(object obj, IBinaryWriter writer)
        {
            if (obj is IBinarizable binarizable)
            {
                binarizable.WriteBinary(writer);
                return;
            }

            foreach (var property in GetTypeData(obj.GetType()).Properties)
            {
                var name = property.Name;
                var value = property.GetValue(obj);
                var rawValue=  Serialize(value);
                writer.WriteObject(name, rawValue);
            }
        }

        public void ReadBinary(object obj, IBinaryReader reader)
        {
            if (obj is IBinarizable binarizable)
            {
                binarizable.ReadBinary(reader);
                return;
            }

            var typeData = GetTypeData(obj.GetType());

            if (typeData.Constructor != null)
            {
                var parameters = typeData.Constructor.GetParameters().Select(p => _services?.GetService(p.ParameterType)).ToArray();
                typeData.Constructor.Invoke(obj, parameters);
            }

            foreach (var property in typeData.Properties)
            {
                try
                {
                    var rawValue = reader.ReadObject<object?>(property.Name);

                    var value = Deserialize(rawValue, property.Type);
                    property.SetValue(obj, value);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed reading value for {obj.GetType()}.{property.Name}", e);
                }
            }
        }
    }
}