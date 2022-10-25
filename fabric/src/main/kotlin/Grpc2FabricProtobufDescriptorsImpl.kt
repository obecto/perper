package com.obecto.perper.fabric
import com.google.protobuf.DynamicMessage
import com.google.protobuf.Empty
import com.google.protobuf.Message
import com.google.protobuf.Any as PbAny

interface PerperProtobufDescriptors {
    suspend fun pack(value: Any): PbAny
    suspend fun unpack(packedValue: PbAny): Any
}

class DummyPerperProtobufDescriptors : PerperProtobufDescriptors {
    override suspend fun pack(value: Any): PbAny {
        return PbAny.pack(if (value is Message) { value } else { Empty.getDefaultInstance() }, "fabric://")
    }
    override suspend fun unpack(packedValue: PbAny): Any {
        return packedValue.unpack(DynamicMessage::class.java)
    }
}
/*
class Grpc2FabricProtobufDescriptorsImpl(val ignite: Ignite) : FabricProtobufDescriptorsKt.FabricProtobufDescriptors() {

}
*/
