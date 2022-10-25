@file:OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
package com.obecto.perper.fabric
import com.google.protobuf.Empty
import com.obecto.perper.model.PerperError
import com.obecto.perper.model.PerperExecutionData
import com.obecto.perper.model.PerperExecutionFilter
import com.obecto.perper.model.PerperExecutions
import com.obecto.perper.protobuf2.ExecutionsCompleteRequest
import com.obecto.perper.protobuf2.ExecutionsCreateRequest
import com.obecto.perper.protobuf2.ExecutionsDeleteRequest
import com.obecto.perper.protobuf2.ExecutionsGetResultRequest
import com.obecto.perper.protobuf2.ExecutionsGetResultResponse
import com.obecto.perper.protobuf2.ExecutionsListenAndReserveRequest
import com.obecto.perper.protobuf2.ExecutionsListenRequest
import com.obecto.perper.protobuf2.ExecutionsListenResponse
import com.obecto.perper.protobuf2.ExecutionsReserveRequest
import com.obecto.perper.protobuf2.FabricExecutionsGrpcKt
import kotlinx.coroutines.Job
import kotlinx.coroutines.awaitCancellation
import kotlinx.coroutines.channels.ProducerScope
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.channelFlow
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking

private fun ExecutionsListenRequest.toFilter() = PerperExecutionFilter(
    agent = instanceFilter.agent,
    instance = instanceFilter.instance,
    delegate = delegate,
    localToData = localToData
)

private suspend fun PerperExecutionData.toListenResponse(perperProtobufDescriptors: PerperProtobufDescriptors, cancelled: Boolean = false) = ExecutionsListenResponse.newBuilder().also {
    it.execution = execution
    it.instance = instance
    it.delegate = delegate
    if (cancelled) {
        it.deleted = true
    } else {
        it.addAllArguments(arguments.map({ x -> perperProtobufDescriptors.pack(x) }))
    }
}.build()

private suspend fun Pair<Array<Any>, PerperError?>?.toGetResultResponse(perperProtobufDescriptors: PerperProtobufDescriptors) = ExecutionsGetResultResponse.newBuilder().also {
    if (this == null) {
        it.deleted = true
    } else {
        it.addAllResults(first.map({ x -> perperProtobufDescriptors.pack(x) }))
        it.error = second
    }
}.build()

private fun Unit.toEmpty() = Empty.getDefaultInstance()

private suspend fun ProducerScope<ExecutionsListenResponse>.sendExecution(execution: PerperExecutionData?, perperProtobufDescriptors: PerperProtobufDescriptors) {
    if (execution == null) {
        send(
            ExecutionsListenResponse.newBuilder().also {
                it.startOfStream = true
            }.build()
        )
    } else {
        send(execution.toListenResponse(perperProtobufDescriptors, false))
        execution.invokeOnCancellation {
            runBlocking { send(execution.toListenResponse(perperProtobufDescriptors, true)) }
        }
    }
}

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
class Grpc2FabricExecutionsImpl(
    val perperExecutions: PerperExecutions,
    val perperProtobufDescriptors: PerperProtobufDescriptors
) : FabricExecutionsGrpcKt.FabricExecutionsCoroutineImplBase() {

    // val log = ignite.log()

    override suspend fun create(request: ExecutionsCreateRequest): Empty {
        perperExecutions.create(
            instance = request.instance,
            delegate = request.delegate,
            arguments = request.argumentsList.map({ x -> perperProtobufDescriptors.unpack(x) }).toTypedArray(),
            execution = request.execution!!,
        )
        return Empty.getDefaultInstance()
    }

    override fun listen(request: ExecutionsListenRequest) = channelFlow<ExecutionsListenResponse> {
        val executions = perperExecutions.listen(request.toFilter())
        executions.collect { execution ->
            sendExecution(execution, perperProtobufDescriptors)
        }
    }

    override fun reserve(request: ExecutionsReserveRequest) = flow<Empty> {
        perperExecutions.reserve(execution = request.execution, workgroup = request.workgroup) {
            emit(Empty.getDefaultInstance())
            awaitCancellation()
        }
    }

    override fun listenAndReserve(requests: Flow<ExecutionsListenAndReserveRequest>) = channelFlow<ExecutionsListenResponse> {
        val semaphore = SemaphoreWithArbitraryRelease(0)
        var executionsJob: Job? = null
        requests.collect { request ->
            semaphore.release(request.reserveNext)

            if (executionsJob == null) {
                executionsJob = launch {
                    semaphore.acquire(1) // Acquiring before we start listening, because perperExecutions.listen will instantly start reserving executions
                    val executions = perperExecutions.listen(request.filter.toFilter().copy(reserveAsWorkgroup = request.workgroup))
                    executions.collect { execution ->
                        if (execution != null) {
                            sendExecution(execution, perperProtobufDescriptors)
                            semaphore.acquire(1) // Acquiring for next iteration
                        }
                    }
                }
            }
        }
    }

    override suspend fun complete(request: ExecutionsCompleteRequest): Empty {
        perperExecutions.complete(
            execution = request.execution,
            results = request.resultsList.map({ x -> perperProtobufDescriptors.unpack(x) }).toTypedArray(),
            error = request.error
        )
        return Empty.getDefaultInstance()
    }

    override suspend fun getResult(request: ExecutionsGetResultRequest) =
        perperExecutions.getResult(execution = request.execution).toGetResultResponse(perperProtobufDescriptors)

    override suspend fun delete(request: ExecutionsDeleteRequest) = perperExecutions.delete(execution = request.execution).toEmpty()
}
