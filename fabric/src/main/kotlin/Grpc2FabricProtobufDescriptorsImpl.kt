package com.obecto.perper.fabric
import com.google.protobuf.BoolValue
import com.google.protobuf.ByteString
import com.google.protobuf.BytesValue
import com.google.protobuf.DescriptorProtos
import com.google.protobuf.Descriptors
import com.google.protobuf.DoubleValue
import com.google.protobuf.DynamicMessage
import com.google.protobuf.Empty
import com.google.protobuf.Enum
import com.google.protobuf.EnumValue
import com.google.protobuf.Field
import com.google.protobuf.FloatValue
import com.google.protobuf.Int32Value
import com.google.protobuf.Int64Value
import com.google.protobuf.Message
import com.google.protobuf.MessageOrBuilder
import com.google.protobuf.StringValue
import com.google.protobuf.Syntax
import com.google.protobuf.Type
import com.google.protobuf.UInt32Value
import com.google.protobuf.UInt64Value
import com.obecto.perper.model.IgniteAny
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsGetRequest
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsGetResponse
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsGrpcKt
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsRegisterRequest
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.map
import org.apache.ignite.Ignite
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.binary.BinaryType
import java.util.UUID
import com.google.protobuf.Any as PbAny

interface ProtobufConverter {
    suspend fun pack(value: IgniteAny?): PbAny
    suspend fun unpack(packedValue: PbAny): IgniteAny?
}

class DummyProtobufConverter : ProtobufConverter { // HACK
    override suspend fun pack(value: IgniteAny?): PbAny =
        when (val wrapped = value?.wrappedValue) {
            is PbAny -> wrapped
            is Message -> PbAny.pack(wrapped, "x/")
            else -> PbAny.getDefaultInstance()
        }

    override suspend fun unpack(packedValue: PbAny): IgniteAny? = IgniteAny(packedValue)
}

class Grpc2FabricProtobufDescriptorsImpl(val ignite: Ignite) : FabricProtobufDescriptorsGrpcKt.FabricProtobufDescriptorsCoroutineImplBase(), ProtobufConverter {
    val binary = ignite.binary()
    val typeUrlsCache = ignite.getOrCreateCache<String, String>("typeUrls")
    val descriptorsCache = ignite.getOrCreateCache<String, ByteArray>("descriptors")
    val wellKnownValueDescriptors = setOf(
        Int32Value.getDescriptor(),
        Int64Value.getDescriptor(),
        UInt32Value.getDescriptor(),
        UInt64Value.getDescriptor(),
        BoolValue.getDescriptor(),
        DoubleValue.getDescriptor(),
        FloatValue.getDescriptor(),
        StringValue.getDescriptor(),
        BytesValue.getDescriptor()
    )
    val wellKnownDescriptors = wellKnownValueDescriptors + setOf()
    val wellKnownDescriptorsMap = wellKnownValueDescriptors.map({ Pair(getTypeUrl(it), it) }).toMap()

    override suspend fun register(request: FabricProtobufDescriptorsRegisterRequest): Empty {
        typeUrlsCache.putAsync(request.typeUrl.substring(request.typeUrl.lastIndexOf('.') + 1), request.typeUrl)
        descriptorsCache.putAsync(request.typeUrl, request.toByteArray()).await()
        return Empty.getDefaultInstance()
    }

    override suspend fun get(request: FabricProtobufDescriptorsGetRequest): FabricProtobufDescriptorsGetResponse {
        val bytes = descriptorsCache.getAsync(request.typeUrl).await()
        if (bytes != null) {
            return FabricProtobufDescriptorsGetResponse.parseFrom(bytes) // Rude casting, but it should Just Work" for now
        } else {
            return FabricProtobufDescriptorsGetResponse.getDefaultInstance()
        }
    }

    suspend fun getEnum(typeUrl: String): Enum {
        val bytes = descriptorsCache.getAsync(typeUrl).await()
        return FabricProtobufDescriptorsGetResponse.parseFrom(bytes).enum
    }

    suspend fun getType(typeUrl: String): Type {
        val bytes = descriptorsCache.getAsync(typeUrl).await()
        if (bytes != null) {
            return FabricProtobufDescriptorsGetResponse.parseFrom(bytes).type
        } else {
            return Type.newBuilder().also {
                it.name = typeUrl.substring(typeUrl.lastIndexOf('/') + 1)
                // TODO
            }.build()
        }
    }

    suspend fun getTypeUrl(binaryType: BinaryType): String {
        return typeUrlsCache.getAsync(binaryType.typeName()).await() ?: "x/" + binaryType.typeName()
    }

    fun getTypeUrl(descriptor: Descriptors.Descriptor): String {
        return if (descriptor.file.`package` == "google.protobuf") { "type.googleapis.com/" } else { "x/" } + descriptor.fullName
    }

    private suspend fun packToField(value: Any, builder: DynamicMessage.Builder, field: Descriptors.FieldDescriptor) {
        if (field.isRepeated() && value is Iterable<*>) {
            for (subValue in value) {
                builder.addRepeatedField(field, packToField(subValue!!, field))
            }
        } else {
            builder.setField(field, packToField(value, field))
        }
    }

    private suspend fun packToField(value: Any, field: Descriptors.FieldDescriptor): Any =
        when (field.javaType!!) {
            Descriptors.FieldDescriptor.JavaType.BOOLEAN -> packToBoolean(value)
            Descriptors.FieldDescriptor.JavaType.BYTE_STRING -> packToByteString(value)
            Descriptors.FieldDescriptor.JavaType.DOUBLE -> packToDouble(value)
            Descriptors.FieldDescriptor.JavaType.ENUM -> field.getEnumType().findValueByNumber(packToInt(value))
            Descriptors.FieldDescriptor.JavaType.FLOAT -> packToFloat(value)
            Descriptors.FieldDescriptor.JavaType.INT -> packToInt(value)
            Descriptors.FieldDescriptor.JavaType.LONG -> packToLong(value)
            Descriptors.FieldDescriptor.JavaType.MESSAGE -> packToMessage(value, field.messageType)
            Descriptors.FieldDescriptor.JavaType.STRING -> packToString(value)
        }

    private fun packToBoolean(value: Any): Boolean =
        when (value) {
            is Long -> value != 0
            is Int -> value != 0
            is Boolean -> value
            is BinaryObject -> value.enumOrdinal() != 0
            else -> throw UnsupportedOperationException("Unexpected value $value for Boolean field")
        }
    private fun packToInt(value: Any): Int =
        when (value) {
            is Long -> value.toInt()
            is Int -> value
            is BinaryObject -> value.enumOrdinal()
            is Boolean -> if (value) 1 else 0
            else -> throw UnsupportedOperationException("Unexpected value $value for Int field")
        }
    private fun packToLong(value: Any): Long =
        when (value) {
            is Long -> value
            is Int -> value.toLong()
            is BinaryObject -> value.enumOrdinal().toLong()
            is Boolean -> if (value) 1 else 0
            else -> throw UnsupportedOperationException("Unexpected value $value for Long field")
        }
    private fun packToByteString(value: Any): ByteString =
        when (value) {
            is ByteArray -> ByteString.copyFrom(value)
            else -> throw UnsupportedOperationException("Unexpected value $value for ByteString field")
        }
    private fun packToString(value: Any): String =
        when (value) {
            is String -> value
            else -> throw UnsupportedOperationException("Unexpected value $value for String field")
        }
    private fun packToDouble(value: Any): Double =
        when (value) {
            is Double -> value
            else -> throw UnsupportedOperationException("Unexpected value $value for Double field")
        }
    private fun packToFloat(value: Any): Float =
        when (value) {
            is Float -> value
            else -> throw UnsupportedOperationException("Unexpected value $value for Float field")
        }

    private suspend fun packToAny(value: Any?): PbAny =
        if (value is PbAny) {
            value
        } else if (value == null) {
            PbAny.getDefaultInstance()
        } else {
            val message = when (value) {
                is Message -> value
                is Long -> Int64Value.of(value)
                is Int -> Int32Value.of(value)
                is Byte -> Int32Value.of(value.toInt())
                is Boolean -> BoolValue.of(value)
                is Double -> DoubleValue.of(value)
                is Float -> FloatValue.of(value)
                is ByteArray -> BytesValue.of(ByteString.copyFrom(value))
                is String -> StringValue.of(value)
                is BinaryObject -> if (value.type().isEnum()) {
                    EnumValue.newBuilder().also {
                        it.name = value.enumName()
                        it.number = value.enumOrdinal()
                    }.build()
                } else {
                    packBinaryObjectToMessage(value, getMessageDescriptor(getTypeUrl(value.type())))
                }
                else -> throw UnsupportedOperationException("Unexpected value $value to convert to Any")
            }
            PbAny.newBuilder().also {
                it.value = message.toByteString()
                it.typeUrl = getTypeUrl(message.descriptorForType)
            }.build()
        }

    private suspend fun packBinaryObjectToMessage(value: BinaryObject, descriptor: Descriptors.Descriptor): DynamicMessage {
        val binaryType = value.type()
        val messageBuilder = DynamicMessage.newBuilder(descriptor)
        for (field in binaryType.fieldNames()) {
            if (value.hasField(field)) {
                val fieldDescriptor = descriptor.findFieldByName(field)
                if (fieldDescriptor != null) {
                    val subValue = value.field<Any>(field)
                    if (subValue != null) {
                        packToField(subValue, messageBuilder, fieldDescriptor)
                    }
                }
            }
        }
        return messageBuilder.build()
    }

    private suspend fun packToMessage(value: Any?, descriptor: Descriptors.Descriptor): Message =
        if (descriptor == PbAny.getDescriptor()) {
            packToAny(value)
        } else if (value is BinaryObject) {
            packBinaryObjectToMessage(value, descriptor)
        } else if (value == null) {
            DynamicMessage.getDefaultInstance(descriptor)
        } else if (descriptor in wellKnownValueDescriptors) {
            DynamicMessage.newBuilder(descriptor).also {
                packToField(value, it, descriptor.getFields().single())
            }.build()
        } else {
            throw UnsupportedOperationException("Can't turn value $value into descriptor $descriptor")
        }

    override suspend fun pack(value: IgniteAny?): PbAny =
        packToAny(value?.wrappedValue)

    override suspend fun unpack(packedValue: PbAny): IgniteAny? {
        return IgniteAny(unpackFromAny(packedValue.typeUrl, packedValue.value))
    }

    private suspend fun unpackFromAny(typeUrl: String, value: ByteString): Any? {
        if (typeUrl == "") return null

        val descriptor = getMessageDescriptor(typeUrl)
        val message = DynamicMessage.parseFrom(descriptor, value)
        return unpackFromMessage(message)
    }

    private suspend fun unpackFromMessage(packedValue: MessageOrBuilder): Any? =
        when (packedValue.descriptorForType) {
            in wellKnownValueDescriptors -> unpackFromField(packedValue.getField(packedValue.descriptorForType.getFields().single()))
            EnumValue.getDescriptor() ->
                binary.buildEnum(
                    packedValue.getField(EnumValue.getDescriptor().findFieldByNumber(EnumValue.NAME_FIELD_NUMBER)) as String,
                    packedValue.getField(EnumValue.getDescriptor().findFieldByNumber(EnumValue.NUMBER_FIELD_NUMBER)) as Int
                )
            PbAny.getDescriptor() ->
                unpackFromAny(
                    packedValue.getField(PbAny.getDescriptor().findFieldByNumber(PbAny.TYPE_URL_FIELD_NUMBER)) as String,
                    packedValue.getField(PbAny.getDescriptor().findFieldByNumber(PbAny.VALUE_FIELD_NUMBER)) as ByteString
                )
            else ->
                binary.builder(packedValue.descriptorForType.fullName).also {
                    for ((fieldDescriptor, subPackedValue) in packedValue.getAllFields()) {
                        val subValue = unpackFromField(subPackedValue)
                        it.setField(fieldDescriptor.name, subValue)
                    }
                }.build()
        }

    private suspend fun unpackFromField(packedValue: Any?): Any? =
        when (packedValue) {
            is List<*> -> coroutineScope { packedValue.map { async { unpackFromField(it) } }.awaitAll() }
            is Descriptors.EnumValueDescriptor -> binary.buildEnum(packedValue.name, packedValue.number)
            is MessageOrBuilder -> unpackFromMessage(packedValue)
            is ByteString -> packedValue.toByteArray()
            else -> packedValue // Primitive
        }

    suspend fun getMessageDescriptor(typeUrl: String): Descriptors.Descriptor {
        val wellKnownDescriptor = wellKnownDescriptorsMap.get(typeUrl)
        if (wellKnownDescriptor != null) {
            return wellKnownDescriptor
        }

        val type = getType(typeUrl)
        val fileProto = DescriptorProtos.FileDescriptorProto.newBuilder()
        val packageDot = type.name.lastIndexOf('.')
        val typePackage = if (packageDot != -1) { type.name.substring(0, packageDot) } else { "" }
        val typeName = type.name.substring(packageDot + 1)
        fileProto.syntax = when (type.syntax) {
            Syntax.SYNTAX_PROTO2 -> "proto2"
            Syntax.SYNTAX_PROTO3 -> "proto3"
            else -> throw IllegalArgumentException("Type syntax must be either proto2 or proto3")
        }
        fileProto.name = "${UUID.randomUUID()}.proto"
        fileProto.setPackage(typePackage)

        val dependenciesSet = HashSet<Descriptors.FileDescriptor>()

        fileProto.addMessageTypeBuilder().also { messageProto ->
            messageProto.name = typeName

            for (field in type.fieldsList) {
                messageProto.addFieldBuilder().also {
                    it.name = field.name
                    it.number = field.number
                    it.label = when (field.cardinality) {
                        Field.Cardinality.CARDINALITY_REPEATED -> DescriptorProtos.FieldDescriptorProto.Label.LABEL_REPEATED
                        Field.Cardinality.CARDINALITY_OPTIONAL -> DescriptorProtos.FieldDescriptorProto.Label.LABEL_OPTIONAL
                        else -> DescriptorProtos.FieldDescriptorProto.Label.LABEL_REQUIRED
                    }

                    it.type = when (field.kind) {
                        Field.Kind.TYPE_DOUBLE -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_DOUBLE
                        Field.Kind.TYPE_FLOAT -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_FLOAT
                        Field.Kind.TYPE_INT64 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_INT64
                        Field.Kind.TYPE_UINT64 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_UINT64
                        Field.Kind.TYPE_INT32 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_INT32
                        Field.Kind.TYPE_FIXED64 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_FIXED64
                        Field.Kind.TYPE_FIXED32 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_FIXED32
                        Field.Kind.TYPE_BOOL -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_BOOL
                        Field.Kind.TYPE_STRING -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_STRING
                        Field.Kind.TYPE_GROUP -> {
                            dependenciesSet.add(getMessageDescriptor(field.typeUrl).file) // TODO: Recursion check
                            DescriptorProtos.FieldDescriptorProto.Type.TYPE_GROUP
                        }
                        Field.Kind.TYPE_MESSAGE -> {
                            dependenciesSet.add(getMessageDescriptor(field.typeUrl).file) // TODO: Recursion check
                            DescriptorProtos.FieldDescriptorProto.Type.TYPE_MESSAGE
                        }
                        Field.Kind.TYPE_BYTES -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_BYTES
                        Field.Kind.TYPE_UINT32 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_UINT32
                        Field.Kind.TYPE_ENUM -> {
                            // dependenciesSet.Add(getEnumDescriptor(field.typeUrl).file)
                            DescriptorProtos.FieldDescriptorProto.Type.TYPE_ENUM
                        }
                        Field.Kind.TYPE_SFIXED32 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_SFIXED32
                        Field.Kind.TYPE_SFIXED64 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_SFIXED64
                        Field.Kind.TYPE_SINT32 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_SINT32
                        Field.Kind.TYPE_SINT64 -> DescriptorProtos.FieldDescriptorProto.Type.TYPE_SINT64
                        else -> throw IllegalArgumentException("Invalid field kind: ${field.kind}")
                    }

                    if (field.typeUrl != "") it.typeName = field.typeUrl.substring(field.typeUrl.lastIndexOf('/'))
                    if (field.defaultValue != "") it.defaultValue = field.defaultValue
                    if (field.oneofIndex > 0) it.oneofIndex = field.oneofIndex - 1
                    if (field.jsonName != "") it.jsonName = field.jsonName
                    it.optionsBuilder.packed = field.packed
                }
            }

            for (oneof in type.oneofsList) {
                messageProto.addOneofDeclBuilder().also { it.name = oneof }
            }
        }

        var dependencies = dependenciesSet.toTypedArray()

        fileProto.addAllDependency(dependencies.map { it.name })

        val file = Descriptors.FileDescriptor.buildFrom(fileProto.build(), dependencies)

        return file.messageTypes[0]
    }

    suspend fun getEnumDescriptor(typeUrl: String): Descriptors.EnumDescriptor {
        val enum = getEnum(typeUrl)
        throw UnsupportedOperationException("Sorry! $enum")
    }
}
