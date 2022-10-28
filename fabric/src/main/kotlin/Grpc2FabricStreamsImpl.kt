@file:OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
package com.obecto.perper.fabric
import com.google.protobuf.Empty
import com.obecto.perper.model.PerperStreamItemFilter
import com.obecto.perper.model.PerperStreams
import com.obecto.perper.protobuf2.FabricStreamsGrpcKt
import com.obecto.perper.protobuf2.StreamsCreateRequest
import com.obecto.perper.protobuf2.StreamsDeleteRequest
import com.obecto.perper.protobuf2.StreamsListenItemsRequest
import com.obecto.perper.protobuf2.StreamsListenItemsResponse
import com.obecto.perper.protobuf2.StreamsMoveListenerRequest
import com.obecto.perper.protobuf2.StreamsWaitForListenerRequest
import com.obecto.perper.protobuf2.StreamsWriteItemRequest
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.flow

private fun String.toNullIfEmpty(): String? = if (this.isEmpty()) { null } else { this }

private fun Unit.toEmpty() = Empty.getDefaultInstance()

private fun StreamsListenItemsRequest.toFilter() = PerperStreamItemFilter(
    stream = stream,
    startFromListener = listenerName.toNullIfEmpty(),
    startFromLatest = startFromLatest,
    localToData = localToData
)

private suspend fun Pair<Long, Any>.toListenItemsResponse(perperProtobufDescriptors: PerperProtobufDescriptors) = StreamsListenItemsResponse.newBuilder().also {
    it.key = first
    it.value = perperProtobufDescriptors.pack(second)
}.build()

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
class Grpc2FabricStreamsImpl(
    val perperStreams: PerperStreams,
    val perperProtobufDescriptors: PerperProtobufDescriptors
) : FabricStreamsGrpcKt.FabricStreamsCoroutineImplBase() {

    // val log = ignite.log().getLogger(this)

    override suspend fun create(request: StreamsCreateRequest): Empty {
        perperStreams.create(
            stream = request.stream,
            cacheOptions = request.cacheOptions,
            ephemeral = request.ephemeral,
        )
        return Empty.getDefaultInstance()
    }

    override fun listenItems(request: StreamsListenItemsRequest) = flow<StreamsListenItemsResponse> {
        perperStreams.listenItems(request.toFilter()).collect { item ->
            emit(item.toListenItemsResponse(perperProtobufDescriptors))
        }
    }

    override suspend fun waitForListener(request: StreamsWaitForListenerRequest) =
        perperStreams.waitForListener(request.stream).toEmpty()

    override suspend fun writeItem(request: StreamsWriteItemRequest) =
        perperStreams.writeItem(request.stream, request.value, if (request.autoKey) { null } else { request.key }).toEmpty()

    override suspend fun moveListener(request: StreamsMoveListenerRequest) =
        perperStreams.updateListener(request.stream, request.listenerName, if (request.deleteListener) { null } else { request.reachedKey }).toEmpty()

    override suspend fun delete(request: StreamsDeleteRequest) = perperStreams.delete(stream = request.stream).toEmpty()
}
