package com.obecto.perper.fabric
import org.apache.commons.lang3.reflect.TypeUtils
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinarySerializer
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable
import java.lang.Class
import java.lang.reflect.Field
import java.lang.reflect.Method
import java.lang.reflect.Type
import java.math.BigDecimal
import java.sql.Time
import java.sql.Timestamp
import java.util.Date
import java.util.UUID

private val objectClass = Any::class.javaObjectType
private val objectArrayType = TypeUtils.genericArrayType(objectClass)
private val collectionClass = Collection::class.javaObjectType
private val collectionClassElementType = collectionClass.typeParameters[0]
private val mapClass = Map::class.javaObjectType
private val mapClassKeyType = mapClass.typeParameters[0]
private val mapClassValueType = mapClass.typeParameters[1]
private val pairClass = Pair::class.javaObjectType

class PerperBinarySerializer : BinarySerializer {
    private interface PropertyInfo {
        val type: Class<*>
        val genericType: Type
        fun get(obj: Any): Any?
        fun set(obj: Any, value: Any?)
    }

    private class PropertyInfoField(val field: Field) : PropertyInfo {
        override val type = field.type
        override val genericType = field.genericType
        override fun get(obj: Any) = field.get(obj)
        override fun set(obj: Any, value: Any?) { field.set(obj, value) }
    }

    private class PropertyInfoGetterSetter(val getter: Method?, val setter: Method?) : PropertyInfo {
        override val type = (getter?.returnType ?: setter?.parameterTypes?.get(0))!!
        override val genericType = (setter?.genericParameterTypes?.get(0) ?: getter?.genericReturnType)!!
        override fun get(obj: Any) = getter?.invoke(obj)
        override fun set(obj: Any, value: Any?) { setter?.invoke(obj, value) }
    }

    private val propertiesCache = HashMap<Class<*>, HashMap<String, PropertyInfo>>()

    private fun getProperties(type: Class<*>) = propertiesCache.getOrPut(
        type,
        {
            val result = HashMap<String, PropertyInfo>()

            for (field in type.fields) {
                result[field.name] = PropertyInfoField(field)
            }

            val getters = HashMap<String, Method>()
            val setters = HashMap<String, Method>()

            for (method in type.methods) {
                if (method.name.startsWith("get") && method.parameterCount == 0 && method.returnType != Void::class.javaPrimitiveType) {
                    getters[method.name.drop(3).decapitalize()] = method
                }
                if (method.name.startsWith("is") && method.parameterCount == 0 && method.returnType == Boolean::class.javaPrimitiveType) {
                    getters[method.name.drop(2).decapitalize()] = method
                }
                if (method.name.startsWith("set") && method.parameterCount == 1) {
                    setters[method.name.drop(3).decapitalize()] = method
                }
            }

            for ((name, getter) in getters) {
                result[name] = PropertyInfoGetterSetter(getter, setters.remove(name))
            }

            for ((name, setter) in setters) {
                result[name] = PropertyInfoGetterSetter(null, setter)
            }

            result
        }
    )

    @Suppress("UNCHECKED_CAST")
    private fun convertCollections(toCommon: Boolean, type: Type, value: Any?): Any? {
        if (value == null) {
            return null
        }
        val mapTypeArguments = TypeUtils.getTypeArguments(type, mapClass)
        if (mapTypeArguments != null) {
            val keyType = mapTypeArguments[mapClassKeyType] ?: objectClass
            val valueType = mapTypeArguments[mapClassValueType] ?: objectClass
            val map = if (toCommon) {
                null
            } else {
                val wantedClass = TypeUtils.getRawType(type, null)
                if (wantedClass.isAssignableFrom(HashMap::class.java)) null else  wantedClass.getConstructor()?.newInstance() as? MutableMap<Any?, Any?>
            }
            return (value as Map<*, *>)
                .mapKeys({ convertCollections(toCommon, keyType, it) })
                .mapValuesTo(map ?: HashMap(), { convertCollections(toCommon, valueType, it) })
        }
        val collectionTypeArguments = TypeUtils.getTypeArguments(type, collectionClass)
        if (collectionTypeArguments != null) {
            val elementType = collectionTypeArguments[collectionClassElementType] ?: objectClass
            val collection = if (toCommon) {
                null
            } else {
                val wantedClass = TypeUtils.getRawType(type, null)
                if (wantedClass.isAssignableFrom(ArrayList::class.java)) null else wantedClass.getConstructor()?.newInstance() as? MutableCollection<Any?>
            }
            return (value as Collection<*>)
                .mapTo(collection ?: ArrayList(), { convertCollections(toCommon, elementType, it) })
        }
        val arrayComponentType = TypeUtils.getArrayComponentType(type)
        if (arrayComponentType != null && !(TypeUtils.getRawType(arrayComponentType, null)?.isPrimitive ?: false)) {
            return (value as Array<*>)
                .map({ convertCollections(toCommon, arrayComponentType, it) }).toTypedArray()
        }
        return value
    }

    @Suppress("UNCHECKED_CAST")
    override fun writeBinary(obj: Any, writer: BinaryWriter) {
        if (obj is Binarylizable) {
            obj.writeBinary(writer)
            return
        }

        for ((name, prop) in getProperties(obj.javaClass)) {
            val value = prop.get(obj)
            if (prop.type.isArray()) {
                when (prop.type.componentType) {
                    Boolean::class.javaPrimitiveType -> writer.writeBooleanArray(name, value as BooleanArray)
                    Char::class.javaPrimitiveType -> writer.writeCharArray(name, value as CharArray)
                    Byte::class.javaPrimitiveType -> writer.writeByteArray(name, value as ByteArray)
                    Short::class.javaPrimitiveType -> writer.writeShortArray(name, value as ShortArray)
                    Int::class.javaPrimitiveType -> writer.writeIntArray(name, value as IntArray)
                    Long::class.javaPrimitiveType -> writer.writeLongArray(name, value as LongArray)
                    Float::class.javaPrimitiveType -> writer.writeFloatArray(name, value as FloatArray)
                    Double::class.javaPrimitiveType -> writer.writeDoubleArray(name, value as DoubleArray)
                    BigDecimal::class.javaObjectType -> writer.writeDecimalArray(name, value as Array<BigDecimal>)
                    Date::class.javaObjectType -> writer.writeDateArray(name, value as Array<Date>)
                    Time::class.javaObjectType -> writer.writeTimeArray(name, value as Array<Time>)
                    Timestamp::class.javaObjectType -> writer.writeTimestampArray(name, value as Array<Timestamp>)
                    UUID::class.javaObjectType -> writer.writeUuidArray(name, value as Array<UUID>)
                    String::class.javaObjectType -> writer.writeStringArray(name, value as Array<String>)
                    else -> when {
                        prop.type.componentType.isEnum() -> writer.writeEnumArray(name, value as Array<Enum<*>>)
                        else -> writer.writeObjectArray(name, convertCollections(true, prop.genericType, value) as Array<*>)
                    }
                }
            } else {
                when (prop.type) {
                    Boolean::class.javaPrimitiveType -> writer.writeBoolean(name, value as Boolean)
                    Char::class.javaPrimitiveType -> writer.writeChar(name, value as Char)
                    Byte::class.javaPrimitiveType -> writer.writeByte(name, value as Byte)
                    Short::class.javaPrimitiveType -> writer.writeShort(name, value as Short)
                    Int::class.javaPrimitiveType -> writer.writeInt(name, value as Int)
                    Long::class.javaPrimitiveType -> writer.writeLong(name, value as Long)
                    Float::class.javaPrimitiveType -> writer.writeFloat(name, value as Float)
                    Double::class.javaPrimitiveType -> writer.writeDouble(name, value as Double)
                    BigDecimal::class.javaObjectType -> writer.writeDecimal(name, value as BigDecimal)
                    Date::class.javaObjectType -> writer.writeDate(name, value as Date)
                    Time::class.javaObjectType -> writer.writeTime(name, value as Time)
                    Timestamp::class.javaObjectType -> writer.writeTimestamp(name, value as Timestamp)
                    UUID::class.javaObjectType -> writer.writeUuid(name, value as UUID)
                    String::class.javaObjectType -> writer.writeString(name, value as String)
                    else -> when {
                        prop.type.isEnum() -> writer.writeEnum(name, value as Enum<*>)
                        mapClass.isAssignableFrom(prop.type) -> writer.writeMap(name, convertCollections(true, prop.genericType, value) as Map<Any?, Any?>)
                        collectionClass.isAssignableFrom(prop.type) -> writer.writeCollection(name, convertCollections(true, prop.genericType, value) as Collection<Any?>)
                        // NOTE: C# version supportsarbitrary tuples; here we support only Pair, as java lacks a native tuple type
                        pairClass.isAssignableFrom(prop.type) -> {
                            val pair = value as Pair<*, *>?
                            writer.writeObjectArray(name, convertCollections(true, objectArrayType, if (pair == null) null else arrayOf(pair.first, pair.second)) as Array<*>?)
                        }
                        else -> writer.writeObject(name, value)
                    }
                }
            }
        }
    }

    override fun readBinary(obj: Any, reader: BinaryReader) {
        if (obj is Binarylizable) {
            obj.readBinary(reader)
            return
        }

        @Suppress("UNCHECKED_CAST")
        for ((name, prop) in getProperties(obj.javaClass)) {
            val value = if (prop.type.isArray()) {
                when (prop.type.componentType) {
                    Boolean::class.javaPrimitiveType -> reader.readBooleanArray(name)
                    Char::class.javaPrimitiveType -> reader.readCharArray(name)
                    Byte::class.javaPrimitiveType -> reader.readByteArray(name)
                    Short::class.javaPrimitiveType -> reader.readShortArray(name)
                    Int::class.javaPrimitiveType -> reader.readIntArray(name)
                    Long::class.javaPrimitiveType -> reader.readLongArray(name)
                    Float::class.javaPrimitiveType -> reader.readFloatArray(name)
                    Double::class.javaPrimitiveType -> reader.readDoubleArray(name)
                    BigDecimal::class.javaObjectType -> reader.readDecimalArray(name)
                    Date::class.javaObjectType -> reader.readDateArray(name)
                    Time::class.javaObjectType -> reader.readTimeArray(name)
                    Timestamp::class.javaObjectType -> reader.readTimestampArray(name)
                    UUID::class.javaObjectType -> reader.readUuidArray(name)
                    String::class.javaObjectType -> reader.readStringArray(name)
                    else -> when {
                        prop.type.componentType.isEnum() -> reader.readEnumArray<Enum<*>>(name)
                        else -> convertCollections(false, prop.genericType, reader.readObjectArray(name))
                    }
                }
            } else {
                when (prop.type) {
                    Boolean::class.javaPrimitiveType -> reader.readBoolean(name)
                    Char::class.javaPrimitiveType -> reader.readChar(name)
                    Byte::class.javaPrimitiveType -> reader.readByte(name)
                    Short::class.javaPrimitiveType -> reader.readShort(name)
                    Int::class.javaPrimitiveType -> reader.readInt(name)
                    Long::class.javaPrimitiveType -> reader.readLong(name)
                    Float::class.javaPrimitiveType -> reader.readFloat(name)
                    Double::class.javaPrimitiveType -> reader.readDouble(name)
                    BigDecimal::class.javaObjectType -> reader.readDecimal(name)
                    Date::class.javaObjectType -> reader.readDate(name)
                    Time::class.javaObjectType -> reader.readTime(name)
                    Timestamp::class.javaObjectType -> reader.readTimestamp(name)
                    UUID::class.javaObjectType -> reader.readUuid(name)
                    String::class.javaObjectType -> reader.readString(name)
                    else -> when {
                        prop.type.isEnum() -> reader.readEnum(name)
                        mapClass.isAssignableFrom(prop.type) -> convertCollections(false, prop.genericType, reader.readMap<Any?, Any?>(name))
                        collectionClass.isAssignableFrom(prop.type) -> convertCollections(false, prop.genericType, reader.readCollection<Any?>(name))
                        pairClass.isAssignableFrom(prop.type) -> {
                            var values = convertCollections(false, objectArrayType, reader.readObjectArray(name)) as Array<*>?
                            if (values == null) null else Pair(values[0], values[1])
                        }
                        else -> reader.readObject(name)
                    }
                }
            }
            prop.set(obj, value)
        }
    }
}
