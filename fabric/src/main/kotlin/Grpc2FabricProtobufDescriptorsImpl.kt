package com.obecto.perper.fabric
import com.google.protobuf.Empty
import com.google.protobuf.Message
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsGetRequest
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsGetResponse
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsGrpcKt
import com.obecto.perper.protobuf2.FabricProtobufDescriptorsRegisterRequest
import org.apache.ignite.Ignite
import com.google.protobuf.Any as PbAny

interface PerperProtobufDescriptors {
    suspend fun pack(value: Any): PbAny
    suspend fun unpack(packedValue: PbAny): Any
}

class DummyPerperProtobufDescriptors : PerperProtobufDescriptors {
    override suspend fun pack(value: Any): PbAny {
        return if (value is PbAny) { value } else { PbAny.pack(if (value is Message) { value } else { Empty.getDefaultInstance() }, "fabric://") }
    }
    override suspend fun unpack(packedValue: PbAny): Any {
        return packedValue
    }
}

class Grpc2FabricProtobufDescriptorsImpl(val ignite: Ignite) : FabricProtobufDescriptorsGrpcKt.FabricProtobufDescriptorsCoroutineImplBase() {
    val descriptorsCache = ignite.getOrCreateCache<String, ByteArray>("descriptors")

    override suspend fun register(request: FabricProtobufDescriptorsRegisterRequest): Empty {
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
}
