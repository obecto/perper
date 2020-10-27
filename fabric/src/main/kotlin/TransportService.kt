package com.obecto.perper.fabric
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.StreamNotification
import com.obecto.perper.protobuf.StreamNotificationFilter
import com.obecto.perper.protobuf.WorkerTrigger
import com.obecto.perper.protobuf.WorkerTriggerFilter
import io.grpc.Server
import io.grpc.ServerBuilder
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.emitAll
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteLogger
import org.apache.ignite.cache.affinity.AffinityKey
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceContext
import javax.cache.event.CacheEntryUpdatedListener

class TransportService(var port: Int) : Service {

    @set:LoggerResource
    lateinit var log: IgniteLogger

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    lateinit var streamService: StreamService

    var workerTriggerChannels = HashMap<Channel<WorkerTrigger>, WorkerTriggerFilter>()
    var streamNotificationsChannel = HashMap<String, Channel<Long>>()

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

        override fun workerTriggers(request: WorkerTriggerFilter): Flow<WorkerTrigger> = subscribtion(workerTriggerChannels, request)

        override fun streamNotifications(request: StreamNotificationFilter) = flow<StreamNotification> {
            val stream = request.stream
            val notificationCache = streamService.getNotificationCache(stream)

            val streamNotificationUpdates = Channel<AffinityKey<Long>>(Channel.UNLIMITED)
            val query = ContinuousQuery<AffinityKey<Long>, StreamNotification>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    if (event.value == null) continue // Removed
                    runBlocking { streamNotificationUpdates.send(event.key) }
                }
            }
            query.setLocal(true)
            query.initialQuery = ScanQuery<AffinityKey<Long>, StreamNotification>().also { it.setLocal(true) }
            val queryCursor = notificationCache.query(query)
            log.debug({ "Streams notifications listener started for '${request.stream}'!" })

            suspend fun sendNotification(key: AffinityKey<Long>) {
                log.debug({ "Sending notification for '${request.stream}'.$key" })
                val notification = StreamNotification.newBuilder().also {
                    it.stream = stream
                    it.notificationKey = key.key()
                    when (val affinityKey = key.affinityKey<Any>()) {
                        is String -> it.stringAffinity = affinityKey
                        is Long -> it.intAffinity = affinityKey
                    }
                }.build()
                emit(notification)
            }

            for (event in queryCursor) {
                sendNotification(event.key)
            }
            for (key in streamNotificationUpdates) {
                sendNotification(key)
            }
        }
    }

    lateinit var server: Server

    override fun init(ctx: ServiceContext) {
        streamService = ignite.services().service<StreamService>("StreamService")
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
}
