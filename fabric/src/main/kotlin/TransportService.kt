package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.notification.CallResultNotification
import com.obecto.perper.fabric.cache.notification.Notification
import com.obecto.perper.fabric.cache.notification.StreamItemNotification
import com.obecto.perper.protobuf.CallNotificationFilter
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.StreamNotificationFilter
import com.obecto.perper.protobuf.SystemNotificationFilter
import io.grpc.Server
import io.grpc.ServerBuilder
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.IgniteQueue
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.affinity.AffinityKey
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.QueryCursor
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.configuration.CollectionConfiguration
import org.apache.ignite.lang.IgniteBiPredicate
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceContext
import javax.cache.Cache.Entry
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener
import kotlin.coroutines.resume
import kotlin.coroutines.suspendCoroutine
import com.obecto.perper.protobuf.Notification as NotificationProto

class TransportService(var port: Int) : Service {

    @set:LoggerResource
    lateinit var log: IgniteLogger

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    lateinit var server: Server

    override fun init(ctx: ServiceContext) {
        var serverBuilder = ServerBuilder.forPort(port)
        serverBuilder.addService(FabricImpl())
        // serverBuilder.addService(ExternalScalerImpl())
        server = serverBuilder.build()
    }

    override fun execute(ctx: ServiceContext) {
        server.start()
    }

    override fun cancel(ctx: ServiceContext) {
        server.shutdown()
        server.awaitTermination()
    }

    fun getNotificationCache(agentDelegate: String): IgniteCache<AffinityKey<Long>, Notification> {
        return ignite.getOrCreateCache<AffinityKey<Long>, Notification>("$agentDelegate-\$notifications")
    }

    fun getNotificationQueue(streamName: String): IgniteQueue<AffinityKey<Long>> {
        return ignite.queue<AffinityKey<Long>>("$streamName-\$notifications", 0, CollectionConfiguration())
    }

    inner class FabricImpl : FabricGrpcKt.FabricCoroutineImplBase() {
        fun AffinityKey<Long>.toNotification() = NotificationProto.newBuilder().also {
            it.notificationKey = key()
            when (val affinityKey = affinityKey<Any>()) {
                is String -> it.stringAffinity = affinityKey
                is Long -> it.intAffinity = affinityKey
                else -> log.error("Unexpected affinity type ${affinityKey.javaClass}")
            }
        }.build()

        override fun systemNotifications(request: SystemNotificationFilter) = flow<NotificationProto> {
            // NOTE: This currently returns CallResultNotification-s as well. Could consider dropping them
            val notificationCache = getNotificationCache(request.agentDelegate)

            val systemNotificationUpdates = Channel<AffinityKey<Long>>(Channel.UNLIMITED)
            val query = ContinuousQuery<AffinityKey<Long>, Notification>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    if (event.value == null) {
                        val confirmedNotification = event.oldValue
                        // NOTE: Probably shouldn't need a gRPC call to systemNotifications to confirm stream notifications
                        if (confirmedNotification is StreamItemNotification) {
                            getNotificationQueue(confirmedNotification.stream).remove(event.key)
                            if (confirmedNotification.ephemeral) {
                                val counter = ignite.atomicLong("${confirmedNotification.cache}-${confirmedNotification.index}", 0, true)
                                if (counter.decrementAndGet() == 0L) {
                                    ignite.cache<Long, BinaryObject>(confirmedNotification.cache).withKeepBinary<Long, BinaryObject>().remove(confirmedNotification.index)
                                    counter.close()
                                }
                            }
                        }
                    } else if (event.value !is StreamItemNotification) {
                        runBlocking { systemNotificationUpdates.send(event.key) }
                    }
                }
            }
            query.setLocal(true)
            query.initialQuery = ScanQuery<AffinityKey<Long>, Notification>().also { it.setLocal(true) }
            val queryCursor = notificationCache.query(query)
            log.debug({ "System notifications listener started for '${request.agentDelegate}'!" })

            for (entry in queryCursor) {
                 if (entry.value !is StreamItemNotification) {
                    log.debug({ "Sending system notification for '${request.agentDelegate}': $entry.key" })
                    emit(entry.key.toNotification())
                 }
            }
            for (key in systemNotificationUpdates) {
                log.debug({ "Sending system notification for '${request.agentDelegate}': $key" })
                emit(key.toNotification())
            }
        }

        override fun streamNotifications(request: StreamNotificationFilter) = flow<NotificationProto> {
            val notificationQueue = getNotificationQueue(request.streamName)
            val affinity = ignite.affinity<AffinityKey<Long>>("ExampleCache")
            val queueCache = "ignite-sys-atomic-cache@default-ds-group" // HACK: Need a way to receive queue updates

            suspend fun updateQueue() {
                val next = notificationQueue.peek()
                if (affinity.mapKeyToNode(next)?.isLocal ?: false) {
                    val key = notificationQueue.take() // TODO: Likely shouldn't take before confirmation
                    log.debug({ "Sending notification for '${request.streamName}': $key" })
                    emit(key.toNotification())
                }
            }

            val query = ContinuousQuery<Any?, Any?>()
            query.localListener = CacheEntryUpdatedListener { _ ->
                runBlocking { updateQueue() }
            }

            // FIXME: Should close cursor when done
            ignite.cache<Any?, Any?>(queueCache).query(query)

            log.debug({ "Streams notifications listener started for '${request.streamName}'!" })
        }

        override suspend fun callResultNotification(request: CallNotificationFilter) = suspendCoroutine<NotificationProto> { cont ->
            val callName = request.callName
            val notificationCache = getNotificationCache(request.agentDelegate)
            lateinit var queryCursor: QueryCursor<Entry<AffinityKey<Long>, Notification>>
            val query = ContinuousQuery<AffinityKey<Long>, Notification>()

            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    queryCursor.close()
                    cont.resume(event.key.toNotification())
                }
            }

            query.remoteFilterFactory = Factory<CacheEntryEventFilter<AffinityKey<Long>, Notification>> {
                CacheEntryEventFilter { event ->
                    val value = event.value
                    value is CallResultNotification && value.call == callName
                }
            }

            query.initialQuery = ScanQuery<AffinityKey<Long>, Notification>().also {
                it.filter = IgniteBiPredicate { _, notification -> notification is CallResultNotification && notification.call == callName }
            }

            queryCursor = notificationCache.query(query)
            for (event in queryCursor) {
                queryCursor.close()
                cont.resume(event.key.toNotification())
            }

            log.debug({ "Call result notification listener started for '${request.callName}'!" })
        }
    }

    /*inner class ExternalScalerImpl : ExternalScalerGrpcKt.ExternalScalerCoroutineImplBase() {
        val ScaledObjectRef.delegate
            get() = scalerMetadataMap.getOrDefault("delegate", name)
        val ScaledObjectRef.targetNotifications
            get() = scalerMetadataMap.getOrDefault("targetNotifications", "10").toLong()

        override suspend fun isActive(request: ScaledObjectRef): IsActiveResponse {
            val notificationCache = agentService.getNotificationCache(request.delegate)
            val available = notificationCache.localSizeLong()
            return IsActiveResponse.newBuilder().also {
                it.result = available > 0
            }.build()
        }

        @FlowPreview
        override fun streamIsActive(request: ScaledObjectRef) = flow<Boolean> {
            val notificationCache = agentService.getNotificationCache(request.delegate).withKeepBinary<BinaryObject, BinaryObject>()

            lateinit var queryCursor: QueryCursor<Entry<BinaryObject, BinaryObject>>
            val query = ContinuousQuery<BinaryObject, BinaryObject>().also {
                it.localListener = CacheEntryUpdatedListener {
                    val available = notificationCache.localSizeLong()
                    runBlocking {
                        try {
                            emit(available > 0)
                        } catch (_: CancellationException) {
                            queryCursor.close()
                        }
                    }
                }
                it.setLocal(true)
            }
            queryCursor = notificationCache.query(query)
        }.debounce(100).map({ value ->
            IsActiveResponse.newBuilder().also {
                it.result = value
            }.build()
        })

        override suspend fun getMetricSpec(request: ScaledObjectRef): GetMetricSpecResponse {
            return GetMetricSpecResponse.newBuilder().also {
                it.addMetricSpecsBuilder().also {
                    it.metricName = "${request.delegate}-notifications"
                    it.targetSize = request.targetNotifications
                }
            }.build()
        }

        override suspend fun getMetrics(request: GetMetricsRequest): GetMetricsResponse {
            val notificationCache = agentService.getNotificationCache(request.scaledObjectRef.delegate)
            val available = notificationCache.localSizeLong()

            return GetMetricsResponse.newBuilder().also {
                it.addMetricValuesBuilder().also {
                    it.metricName = "${request.scaledObjectRef.delegate}-notifications"
                    it.metricValue = available
                }
            }.build()
        }
    }*/
}
