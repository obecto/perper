package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.notification.Notification
import com.obecto.perper.fabric.cache.notification.StreamItemNotification
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.NotificationFilter
import io.grpc.Server
import io.grpc.ServerBuilder
import kotlinx.coroutines.CancellationException
import kotlinx.coroutines.FlowPreview
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteLogger
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.affinity.AffinityKey
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.QueryCursor
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.resources.ServiceResource
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceContext
import sh.keda.externalscaler.ExternalScalerGrpcKt
import sh.keda.externalscaler.GetMetricSpecResponse
import sh.keda.externalscaler.GetMetricsRequest
import sh.keda.externalscaler.GetMetricsResponse
import sh.keda.externalscaler.IsActiveResponse
import sh.keda.externalscaler.ScaledObjectRef
import javax.cache.Cache.Entry
import javax.cache.event.CacheEntryUpdatedListener
import com.obecto.perper.protobuf.Notification as NotificationProto

class TransportService(var port: Int) : Service {

    @set:LoggerResource
    lateinit var log: IgniteLogger

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    @set:ServiceResource(serviceName = "AgentService")
    lateinit var agentService: AgentService

    lateinit var server: Server

    override fun init(ctx: ServiceContext) {
        var serverBuilder = ServerBuilder.forPort(port)
        serverBuilder.addService(FabricImpl())
        serverBuilder.addService(ExternalScalerImpl())
        server = serverBuilder.build()
    }

    override fun execute(ctx: ServiceContext) {
        server.start()
    }

    override fun cancel(ctx: ServiceContext) {
        server.shutdown()
        server.awaitTermination()
    }

    inner class FabricImpl : FabricGrpcKt.FabricCoroutineImplBase() {
        override fun notifications(request: NotificationFilter) = flow<NotificationProto> {
            val notificationCache = agentService.getNotificationCache(request.agentDelegate)

            val streamNotificationUpdates = Channel<AffinityKey<Long>>(Channel.UNLIMITED)
            val query = ContinuousQuery<AffinityKey<Long>, Notification>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    if (event.value == null) {
                        val confirmedNotification = event.oldValue
                        if (confirmedNotification is StreamItemNotification && confirmedNotification.ephemeral) {
                            val counter = ignite.atomicLong("${confirmedNotification.cache}-${confirmedNotification.index}", 0, true)
                            if (counter.decrementAndGet() == 0L) {
                                ignite.cache<Long, BinaryObject>(confirmedNotification.cache).withKeepBinary<Long, BinaryObject>().remove(confirmedNotification.index)
                                counter.close()
                            }
                        }
                    } else {
                        runBlocking { streamNotificationUpdates.send(event.key) }
                    }
                }
            }
            query.setLocal(true)
            query.initialQuery = ScanQuery<AffinityKey<Long>, Notification>().also { it.setLocal(true) }
            val queryCursor = notificationCache.query(query)
            log.debug({ "Streams notifications listener started for '${request.agentDelegate}'!" })

            suspend fun sendNotification(key: AffinityKey<Long>) {
                log.debug({ "Sending notification for '${request.agentDelegate}'.$key" })
                val notification = NotificationProto.newBuilder().also {
                    it.notificationKey = key.key()
                    when (val affinityKey = key.affinityKey<Any>()) {
                        is String -> it.stringAffinity = affinityKey
                        is Long -> it.intAffinity = affinityKey
                        else -> log.error("Unexpected affinity type ${affinityKey.javaClass}")
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

    inner class ExternalScalerImpl : ExternalScalerGrpcKt.ExternalScalerCoroutineImplBase() {
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
    }
}
