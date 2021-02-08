package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.notification.CallResultNotification
import com.obecto.perper.fabric.cache.notification.Notification
import com.obecto.perper.fabric.cache.notification.StreamItemNotification
import com.obecto.perper.protobuf.CallNotificationFilter
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.NotificationFilter
import io.grpc.Server
import io.grpc.ServerBuilder
import kotlinx.coroutines.GlobalScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.channelFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.launch
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
import sh.keda.externalscaler.ExternalScalerGrpcKt
import sh.keda.externalscaler.GetMetricSpecResponse
import sh.keda.externalscaler.GetMetricsRequest
import sh.keda.externalscaler.GetMetricsResponse
import sh.keda.externalscaler.IsActiveResponse
import sh.keda.externalscaler.ScaledObjectRef
import java.util.concurrent.CancellationException
import java.util.concurrent.ConcurrentHashMap
import javax.cache.Cache.Entry
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener
import javax.cache.event.EventType
import com.obecto.perper.protobuf.Notification as NotificationProto

class TransportService(var port: Int) : Service {

    companion object Caches {
        fun getNotificationCache(ignite: Ignite, agentDelegate: String): IgniteCache<AffinityKey<Long>, Notification> {
            return ignite.getOrCreateCache<AffinityKey<Long>, Notification>("$agentDelegate-\$notifications")
        }

        fun getNotificationQueue(ignite: Ignite, streamName: String): IgniteQueue<AffinityKey<Long>> {
            return ignite.queue(
                "$streamName-\$notifications", 0,
                CollectionConfiguration().also {
                    it.backups = 1 // Workaround IGNITE-7789
                }
            )
        }
    }

    lateinit var log: IgniteLogger

    lateinit var ignite: Ignite

    lateinit var server: Server

    @IgniteInstanceResource
    fun setIgniteResource(igniteResource: Ignite?) {
        if (igniteResource != null) {
            ignite = igniteResource
        }
    }

    @LoggerResource
    fun setLoggerResource(loggerResource: IgniteLogger?) {
        if (loggerResource != null) {
            log = loggerResource
        }
    }

    override fun init(ctx: ServiceContext) {
        var serverBuilder = ServerBuilder.forPort(port)
        serverBuilder.addService(FabricImpl())
        serverBuilder.addService(ExternalScalerImpl())
        server = serverBuilder.build()
    }

    override fun execute(ctx: ServiceContext) {
        server.start()
        log.debug({ "Transport service started!" })
    }

    override fun cancel(ctx: ServiceContext) {
        server.shutdown()
        server.awaitTermination()
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

        @kotlinx.coroutines.ExperimentalCoroutinesApi
        override fun notifications(request: NotificationFilter) = channelFlow<NotificationProto> {
            val notificationCache = getNotificationCache(ignite, request.agentDelegate)
            val notificationAffinity = ignite.affinity<AffinityKey<Long>>(request.agentDelegate)
            val localNode = ignite.cluster().localNode()
            val finishChannel = Channel<Throwable>(Channel.CONFLATED)

            val sentQueueNotificationsMap = ConcurrentHashMap<String, AffinityKey<Long>>()

            fun updateQueue(stream: String) {
                val queue = getNotificationQueue(ignite, stream)
                val queuedKey: AffinityKey<Long>? = queue.peek()

                if (queuedKey == null) {
                    return
                }

                try {
                    if (!notificationAffinity.isPrimary(localNode, queuedKey)) {
                        return
                    }
                } catch (e: Exception) { } // Necessitated by IGNITE-8978

                if (sentQueueNotificationsMap.put(stream, queuedKey) != queuedKey) {
                    runBlocking { send(queuedKey.toNotification()) }
                }
            }

            fun processNotification(key: AffinityKey<Long>, notification: Notification, confirmed: Boolean) {
                if (notification is CallResultNotification) {
                    // pass, handled by callResultNotification below
                } else if (notification is StreamItemNotification) {
                    if (confirmed) {
                        getNotificationQueue(ignite, notification.stream).remove(key)

                        if (notification.ephemeral) {
                            val counter = ignite.atomicLong("${notification.cache}-${notification.key}", 0, true)
                            if (counter.decrementAndGet() == 0L) {
                                ignite.cache<Long, BinaryObject>(notification.cache).withKeepBinary<Long, BinaryObject>().remove(notification.key)
                                counter.close()
                            }
                        }
                    }
                    updateQueue(notification.stream)
                } else if (!confirmed) {
                    runBlocking { send(key.toNotification()) }
                }
            }

            val remoteConfirmationQuery = ContinuousQuery<AffinityKey<Long>, Notification>()
            remoteConfirmationQuery.localListener = CacheEntryUpdatedListener { events ->
                try {
                    for (event in events) {
                        if (event.eventType == EventType.REMOVED) {
                            val value = event.value ?: event.oldValue
                            updateQueue((value as StreamItemNotification).stream)
                        }
                    }
                } catch (e: Exception) {
                    runBlocking { finishChannel.send(e) }
                }
            }

            remoteConfirmationQuery.remoteFilterFactory = Factory<CacheEntryEventFilter<AffinityKey<Long>, Notification>> {
                CacheEntryEventFilter { event -> event.eventType == EventType.REMOVED && (event.value ?: event.oldValue) is StreamItemNotification }
            }

            val remoteQueryCursor = notificationCache.query(remoteConfirmationQuery)

            val query = ContinuousQuery<AffinityKey<Long>, Notification>()
            query.localListener = CacheEntryUpdatedListener { events ->
                try {
                    for (event in events) {
                        processNotification(event.key, event.value ?: event.oldValue, event.eventType == EventType.REMOVED)
                    }
                } catch (e: Exception) {
                    runBlocking { finishChannel.send(e) }
                }
            }

            query.setLocal(true)
            query.initialQuery = ScanQuery<AffinityKey<Long>, Notification>().also { it.setLocal(true) }
            val queryCursor = notificationCache.query(query)

            log.debug({ "Notifications listener started for '${request.agentDelegate}'!" })

            invokeOnClose({ runBlocking { finishChannel.send(it ?: CancellationException()) } })

            try {
                for (entry in queryCursor) {
                    processNotification(entry.key, entry.value, false)
                }

                throw finishChannel.receive()
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                log.error(e.toString())
                e.printStackTrace()
                throw e
            } finally {
                remoteQueryCursor.close()
                queryCursor.close()

                log.debug({ "Notifications listener finished for '${request.agentDelegate}'!" })
            }
        }

        override suspend fun callResultNotification(request: CallNotificationFilter): NotificationProto {
            val callName = request.callName
            val notificationCache = getNotificationCache(ignite, request.agentDelegate)
            lateinit var queryCursor: QueryCursor<Entry<AffinityKey<Long>, Notification>>
            val query = ContinuousQuery<AffinityKey<Long>, Notification>()
            val resultChannel = Channel<AffinityKey<Long>>(Channel.CONFLATED)

            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    runBlocking { resultChannel.send(event.key) }
                }
            }

            query.remoteFilterFactory = Factory<CacheEntryEventFilter<AffinityKey<Long>, Notification>> {
                CacheEntryEventFilter { event -> (event.value as? CallResultNotification)?.call == callName }
            }

            query.initialQuery = ScanQuery<AffinityKey<Long>, Notification>().also {
                it.filter = IgniteBiPredicate { _, notification -> notification is CallResultNotification && notification.call == callName }
            }

            queryCursor = notificationCache.query(query)
            GlobalScope.launch {
                for (event in queryCursor) {
                    resultChannel.send(event.key)
                }
            }

            log.debug({ "Call result notification listener started for '${request.callName}'!" })

            val result = resultChannel.receive()

            log.debug({ "Call result notification listener completed for '${request.callName}'!" })
            queryCursor.close()
            return result.toNotification()
        }
    }

    inner class ExternalScalerImpl : ExternalScalerGrpcKt.ExternalScalerCoroutineImplBase() {
        val ScaledObjectRef.delegate
            get() = scalerMetadataMap.getOrDefault("delegate", name)
        val ScaledObjectRef.targetNotifications
            get() = scalerMetadataMap.getOrDefault("targetNotifications", "10").toLong()

        override suspend fun isActive(request: ScaledObjectRef): IsActiveResponse {
            val notificationCache = getNotificationCache(ignite, request.delegate)
            val available = notificationCache.localSizeLong()
            return IsActiveResponse.newBuilder().also {
                it.result = available > 0
            }.build()
        }

        @kotlinx.coroutines.FlowPreview
        override fun streamIsActive(request: ScaledObjectRef) = flow<Boolean> {
            val notificationCache = getNotificationCache(ignite, request.delegate).withKeepBinary<BinaryObject, BinaryObject>()

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
            val notificationCache = getNotificationCache(ignite, request.scaledObjectRef.delegate)
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
