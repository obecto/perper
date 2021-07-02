package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.notification.CallResultNotification
import com.obecto.perper.fabric.cache.notification.Notification
import com.obecto.perper.fabric.cache.notification.NotificationKey
import com.obecto.perper.fabric.cache.notification.NotificationKeyLong
import com.obecto.perper.fabric.cache.notification.NotificationKeyString
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
        fun getNotificationCache(ignite: Ignite, agent: String): IgniteCache<NotificationKey, Notification> {
            return ignite.getOrCreateCache<NotificationKey, Notification>("$agent-\$notifications")
        }

        fun getNotificationQueue(ignite: Ignite, streamName: String): IgniteQueue<NotificationKey> {
            return ignite.queue(
                "$streamName-\$notifications", 0,
                CollectionConfiguration().also {
                    it.backups = 1 // Workaround IGNITE-7789
                }
            )
        }

        val startTicks = (System.currentTimeMillis()) * 10_000
        val startNanos = System.nanoTime()

        fun getCurrentTicks() = startTicks + (System.nanoTime() - startNanos) / 100
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
        fun NotificationKey.toNotification() = NotificationProto.newBuilder().also {
            when (this) {
                is NotificationKeyString -> {
                    it.notificationKey = key
                    it.stringAffinity = affinity
                }
                is NotificationKeyLong -> {
                    it.notificationKey = key
                    it.intAffinity = affinity
                }
            }
        }.build()

        @kotlinx.coroutines.ExperimentalCoroutinesApi
        override fun notifications(request: NotificationFilter) = channelFlow<NotificationProto> {
            val notificationCache = getNotificationCache(ignite, request.agent)
            val notificationAffinity = ignite.affinity<NotificationKey>(request.agent)
            val localNode = ignite.cluster().localNode()
            val finishChannel = Channel<Throwable>(Channel.CONFLATED)

            val sentQueueNotificationsMap = ConcurrentHashMap<String, NotificationKey>()

            fun updateQueue(stream: String) {
                val queue = getNotificationQueue(ignite, stream)
                val queuedKey: NotificationKey? = queue.peek()

                if (queuedKey == null) {
                    log.trace({ "No notifications for: ${stream} (${queue.size})" })
                    return
                }

                try {
                    if (!notificationAffinity.isPrimary(localNode, queuedKey)) {
                        return
                    }
                } catch (e: Exception) { } // Necessitated by IGNITE-8978

                if (sentQueueNotificationsMap.put(stream, queuedKey) != queuedKey) {
                    log.trace({ "Sending notification: ${stream} - ${queuedKey} (${queue.size})" })
                    runBlocking { send(queuedKey.toNotification()) }
                }
            }

            fun processNotification(key: NotificationKey, notification: Notification, confirmed: Boolean) {
                if (notification is CallResultNotification) {
                    // pass, handled by callResultNotification below
                } else if (notification is StreamItemNotification) {
                    if (confirmed) {
                        val queue = getNotificationQueue(ignite, notification.stream)
                        log.trace({ "Consume notification start: ${notification.stream} - ${key} (${queue.size})" })
                        queue.remove(key)

                        if (notification.ephemeral) {
                            val counter = ignite.atomicLong("${notification.cache}-${notification.key}", 0, true)
                            if (counter.decrementAndGet() == 0L) {
                                ignite.cache<Long, BinaryObject>(notification.cache).withKeepBinary<Long, BinaryObject>().remove(notification.key)
                                counter.close()
                            }
                        }
                        log.trace({ "Consume notification end: ${notification.stream} - ${key} (${queue.size})" })
                    }
                    updateQueue(notification.stream)
                } else if (!confirmed) {
                    log.trace({ "Sending notification ${request.agent}, ${notification} - ${key}" })
                    runBlocking { send(key.toNotification()) }
                }
            }

            val remoteConfirmationQuery = ContinuousQuery<NotificationKey, Notification>()
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

            remoteConfirmationQuery.remoteFilterFactory = Factory<CacheEntryEventFilter<NotificationKey, Notification>> {
                CacheEntryEventFilter { event -> event.eventType == EventType.REMOVED && (event.value ?: event.oldValue) is StreamItemNotification }
            }

            val remoteQueryCursor = notificationCache.query(remoteConfirmationQuery)

            val query = ContinuousQuery<NotificationKey, Notification>()
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
            query.initialQuery = ScanQuery<NotificationKey, Notification>().also { it.setLocal(true) }
            val queryCursor = notificationCache.query(query)

            log.debug({ "Notifications listener started for '${request.agent}'!" })

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

                log.debug({ "Notifications listener finished for '${request.agent}'!" })
            }
        }

        override suspend fun callResultNotification(request: CallNotificationFilter): NotificationProto {
            val call = request.call
            val notificationCache = getNotificationCache(ignite, request.agent)
            lateinit var queryCursor: QueryCursor<Entry<NotificationKey, Notification>>
            val query = ContinuousQuery<NotificationKey, Notification>()
            val resultChannel = Channel<NotificationKey>(Channel.CONFLATED)

            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    runBlocking { resultChannel.send(event.key) }
                }
            }

            query.remoteFilterFactory = Factory<CacheEntryEventFilter<NotificationKey, Notification>> {
                CacheEntryEventFilter { event -> (event.value as? CallResultNotification)?.call == call }
            }

            query.initialQuery = ScanQuery<NotificationKey, Notification>().also {
                it.filter = IgniteBiPredicate { _, notification -> notification is CallResultNotification && notification.call == call }
            }

            queryCursor = notificationCache.query(query)
            GlobalScope.launch {
                try {
                    for (event in queryCursor) {
                        resultChannel.send(event.key)
                    }
                } catch (e: java.util.NoSuchElementException) {} catch (e: org.apache.ignite.IgniteException) {} catch (e: org.apache.ignite.cache.query.QueryCancelledException) {}
            }

            log.debug({ "Call result notification listener started for '$call'!" })

            val result = resultChannel.receive()

            log.debug({ "Call result notification listener completed for '$call'!" })
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
