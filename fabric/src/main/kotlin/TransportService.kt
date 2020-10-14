package com.obecto.perper.fabric
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.StreamTrigger
import com.obecto.perper.protobuf.StreamTriggerFilter
import com.obecto.perper.protobuf.StreamUpdate
import com.obecto.perper.protobuf.StreamUpdateFilter
import com.obecto.perper.protobuf.WorkerTrigger
import com.obecto.perper.protobuf.WorkerTriggerFilter
import io.grpc.Server
import io.grpc.ServerBuilder
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.emitAll
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.runBlocking
import org.apache.ignite.IgniteLogger
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceContext

class TransportService(var port: Int) : Service {

    @set:LoggerResource
    lateinit var log: IgniteLogger

    var streamTriggerChannels = HashMap<Channel<StreamTrigger>, StreamTriggerFilter>()
    var workerTriggerChannels = HashMap<Channel<WorkerTrigger>, WorkerTriggerFilter>()
    var streamUpdateChannels = HashMap<Channel<StreamUpdate>, StreamUpdateFilter>()

    inner class FabricGrpc : FabricGrpcKt.FabricCoroutineImplBase() {
        fun <F, S> subscribtion(channelMap: MutableMap<Channel<S>, F>, request: F): Flow<S> = flow {
            var channel = Channel<S>(Channel.UNLIMITED)
            var originalValue = channelMap.put(channel, request)
            try {
                emitAll(channel)
            } finally {
                if (originalValue == null) {
                    channelMap.remove(channel)
                } else {
                    channelMap.put(channel, originalValue)
                }
            }
        }

        override fun streamTriggers(request: StreamTriggerFilter): Flow<StreamTrigger> = subscribtion(streamTriggerChannels, request)
        override fun workerTriggers(request: WorkerTriggerFilter): Flow<WorkerTrigger> = subscribtion(workerTriggerChannels, request)
        override fun streamUpdates(request: StreamUpdateFilter): Flow<StreamUpdate> = subscribtion(streamUpdateChannels, request)
    }

    lateinit var server: Server

    override fun init(ctx: ServiceContext) {
        var serverBuilder = ServerBuilder.forPort(port)
        serverBuilder.addService(FabricGrpc())
        server = serverBuilder.build()
    }

    override fun execute(ctx: ServiceContext) {
        server.start()
    }

    override fun cancel(ctx: ServiceContext) {
        server.shutdown()
        server.awaitTermination()
    }

    fun sendStreamTrigger(receiverStream: String, receiverDelegate: String) {
        var trigger = StreamTrigger.newBuilder().also {
            it.delegate = receiverDelegate
            it.stream = receiverStream
        }.build()

        log.debug({ "Sending stream trigger: $trigger" })

        runBlocking {
            for ((channel, filter) in streamTriggerChannels) {
                if (filter.hasDelegate() && filter.delegate != receiverDelegate) continue
                channel.send(trigger)
            }
        }
    }

    fun sendWorkerTrigger(receiverStream: String, workerDelegate: String, workerName: String) {
        var trigger = WorkerTrigger.newBuilder().also {
            it.workerDelegate = workerDelegate
            it.stream = receiverStream
            it.worker = workerName
        }.build()

        log.debug({ "Sending worker trigger: $trigger" })

        runBlocking {
            for ((channel, filter) in workerTriggerChannels) {
                if (filter.hasWorkerDelegate() && filter.workerDelegate != workerDelegate) continue
                channel.send(trigger)
            }
        }
    }

    fun sendWorkerResult(receiverStream: String, receiverDelegate: String, workerName: String) {
        var update = StreamUpdate.newBuilder().also {
            it.delegate = receiverDelegate
            it.stream = receiverStream
            it.workerResult = StreamUpdate.WorkerResult.newBuilder().also {
                it.worker = workerName
            }.build()
        }.build()

        log.debug({ "Sending worker result update: $update" })

        runBlocking {
            for ((channel, filter) in streamUpdateChannels) {
                if (filter.hasDelegate() && filter.delegate != receiverDelegate) continue
                if (filter.hasStream() && filter.stream != receiverStream) continue
                channel.send(update)
            }
        }
    }

    fun sendItemUpdate(receiverStream: String, receiverDelegate: String, receiverParameter: String, senderStream: String, senderItem: Long) {
        var update = StreamUpdate.newBuilder().also {
            it.delegate = receiverDelegate
            it.stream = receiverStream
            it.itemUpdate = StreamUpdate.ItemUpdate.newBuilder().also {
                it.parameter = receiverParameter
                it.parameterStream = senderStream
                it.parameterStreamItem = senderItem
            }.build()
        }.build()

        log.debug({ "Sending stream item update: $update" })

        runBlocking {
            for ((channel, filter) in streamUpdateChannels) {
                if (filter.hasDelegate() && filter.delegate != receiverDelegate) continue
                if (filter.hasStream() && filter.stream != receiverStream) continue
                channel.send(update)
            }
        }
    }
}
