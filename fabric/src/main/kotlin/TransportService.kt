package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.AgentType
import com.obecto.perper.fabric.cache.CallData
import com.obecto.perper.fabric.cache.StreamData
import com.obecto.perper.fabric.cache.StreamDelegateType
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.NotificationFilter
import com.obecto.perper.protobuf.CallFilter
import com.obecto.perper.protobuf.CallTrigger
import com.obecto.perper.protobuf.StreamTrigger
import com.obecto.perper.protobuf.StreamItemFilter
import io.grpc.Server
import io.grpc.ServerBuilder
import kotlinx.coroutines.GlobalScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.buffer
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
/*import sh.keda.externalscaler.ExternalScalerGrpcKt
import sh.keda.externalscaler.GetMetricSpecResponse
import sh.keda.externalscaler.GetMetricsRequest
import sh.keda.externalscaler.GetMetricsResponse
import sh.keda.externalscaler.IsActiveResponse
import sh.keda.externalscaler.ScaledObjectRef*/
import java.util.concurrent.CancellationException
import java.util.concurrent.ConcurrentHashMap
import java.util.concurrent.TimeUnit
import javax.cache.Cache.Entry
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEvent
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener
import javax.cache.event.EventType
import com.google.protobuf.Empty
import com.obecto.perper.protobuf.Notification as NotificationProto

class TransportService(var port: Int = 40400) : Service {

    companion object Ticks {
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
        // serverBuilder.addService(ExternalScalerImpl())
        server = serverBuilder.build()
    }

    override fun execute(ctx: ServiceContext) {
        server.start()
        log.debug({ "Transport service started!" })
    }

    override fun cancel(ctx: ServiceContext) {
        server.shutdown()
        server.awaitTermination(1L, TimeUnit.SECONDS)
        server.shutdownNow()
        server.awaitTermination()
    }

    inner class FabricImpl : FabricGrpcKt.FabricCoroutineImplBase() {
        @OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
        override fun callTriggers(request: NotificationFilter) = channelFlow<CallTrigger> {
            val agent = request.agent
            val instance = if (request.instance != "") request.instance else null
            val callsCacheName = "calls"
            val callsCache = ignite.getOrCreateCache<String, CallData>(callsCacheName)

            val queryChannel = Channel<CacheEntryEvent<out String, out CallData>>(Channel.UNLIMITED)
            val query = ContinuousQuery<String, CallData>()
            query.localListener = CacheEntryUpdatedListener { events ->
                try {
                    for (event in events) {
                        if (event.eventType != EventType.REMOVED) {
                            runBlocking { queryChannel.send(event) }
                        }
                    }
                } catch (e: Exception) {
                    queryChannel.close(e)
                }
            }
            query.setLocal(true)

            query.remoteFilterFactory = Factory<CacheEntryEventFilter<String, CallData>> {
                CacheEntryEventFilter { event ->
                    if (event.eventType == EventType.REMOVED)
                        false
                    else if (event.isOldValueAvailable && !event.oldValue.finished)
                        false
                    else // TODO: Use a single query for all agents
                        event.value.agent == agent && (instance == null || event.value.instance == instance) && !event.value.finished
                }
            }
            query.initialQuery = ScanQuery<String, CallData>().also {
                it.setLocal(true)
                it.filter = IgniteBiPredicate<String, CallData> { _, value ->
                    value.agent == agent && (instance == null || value.instance == instance) && !value.finished
                }
            }

            log.debug({ "Call triggers listener stopped for '${request.agent}'|'${request.instance}'!" })

            val queryCursor = callsCache.query(query)

            try {
                for (entry in queryCursor) {
                    send(CallTrigger.newBuilder().also {
                        it.instance = entry.value.instance
                        it.delegate = entry.value.delegate
                        it.call = entry.key
                    }.build())
                }
                for (entry in queryChannel) {
                    send(CallTrigger.newBuilder().also {
                        it.instance = entry.value.instance
                        it.delegate = entry.value.delegate
                        it.call = entry.key
                    }.build())
                }
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                log.error(e.toString())
                e.printStackTrace()
                throw e
            } finally {
                log.debug({ "Call triggers listener started for '${request.agent}'|'${request.instance}'!" })
                queryCursor.close()
            }
        }.buffer(Channel.UNLIMITED)

        @OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
        override fun streamTriggers(request: NotificationFilter) = channelFlow<StreamTrigger> {
            val agent = request.agent
            val instance = if (request.instance != "") request.instance else null
            val streamsCacheName = "streams"
            val streamsCache = ignite.getOrCreateCache<String, StreamData>(streamsCacheName)

            val queryChannel = Channel<CacheEntryEvent<out String, out StreamData>>(Channel.UNLIMITED)
            val query = ContinuousQuery<String, StreamData>()
            query.localListener = CacheEntryUpdatedListener { events ->
                try {
                    for (event in events) {
                        if (event.eventType != EventType.REMOVED) {
                            runBlocking { queryChannel.send(event) }
                        }
                    }
                } catch (e: Exception) {
                    queryChannel.close(e)
                }
            }
            query.setLocal(true)

            fun shouldTrigger(value: StreamData): Boolean {
                return value.delegateType == StreamDelegateType.Action || (value.delegateType == StreamDelegateType.Function && value.listeners.size > 0)
            }


            query.remoteFilterFactory = Factory<CacheEntryEventFilter<String, StreamData>> {
                CacheEntryEventFilter { event ->
                    if (event.eventType == EventType.REMOVED)
                        false
                    else if (event.value.agent != agent || (instance != null && event.value.instance != instance)) // TODO: Use a single query for all agents
                        false
                    else if (!shouldTrigger(event.value))
                        false
                    else if (event.isOldValueAvailable && shouldTrigger(event.oldValue))
                        false
                    else
                        true
                }
            }
            query.initialQuery = ScanQuery<String, StreamData>().also {
                it.setLocal(true)
                it.filter = IgniteBiPredicate<String, StreamData> { _, value ->
                    if (value.agent != agent || (instance != null && value.instance != instance))
                        false
                    else
                        shouldTrigger(value)
                }
            }

            log.debug({ "Stream triggers listener started for '${request.agent}'|'${request.instance}'!" })

            val queryCursor = streamsCache.query(query)

            try {
                for (entry in queryCursor) {
                    log.debug({ "Starting stream ${entry.key}" })
                    send(StreamTrigger.newBuilder().also {
                        it.instance = entry.value.instance
                        it.delegate = entry.value.delegate
                        it.stream = entry.key
                    }.build())
                }
                for (entry in queryChannel) {
                    log.debug({ "Starting stream ${entry.key}" })
                    send(StreamTrigger.newBuilder().also {
                        it.instance = entry.value.instance
                        it.delegate = entry.value.delegate
                        it.stream = entry.key
                    }.build())
                }
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                log.error(e.toString())
                e.printStackTrace()
                throw e
            } finally {
                log.debug({ "Stream triggers listener finished for '${request.agent}'|'${request.instance}'!" })
                queryCursor.close()
            }
        }.buffer(Channel.UNLIMITED)

        override suspend fun callFinished(request: CallFilter): Empty {
            val call = request.call
            val callsCacheName = "calls"
            val callsCache = ignite.getOrCreateCache<String, CallData>(callsCacheName)

            val queryChannel = Channel<Unit>(Channel.CONFLATED)
            val query = ContinuousQuery<String, CallData>()

            query.localListener = CacheEntryUpdatedListener { events ->
                try {
                    for (event in events) {
                        runBlocking { queryChannel.send(Unit) }
                    }
                } catch (e: Exception) {
                    queryChannel.close(e)
                }
            }

            query.remoteFilterFactory = Factory<CacheEntryEventFilter<String, CallData>> {
                CacheEntryEventFilter { event ->
                    event.eventType != EventType.REMOVED && event.key == call && event.value.finished
                }
            }

            val queryCursor = callsCache.query(query) // Important to start query before checking if it is already finished

            log.debug({ "Call result notification listener started for '$call'!" })

            try {
                val callData = callsCache.get(call)
                if (callData == null || !callData.finished)
                {
                    queryChannel.receive()
                }
                return Empty.getDefaultInstance()
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                log.error(e.toString())
                e.printStackTrace()
                throw e
            } finally {
                log.debug({ "Call result notification listener completed for '$call'!" })
                queryCursor.close()
                queryChannel.close()
            }
        }

        override suspend fun streamItemWritten(request: StreamItemFilter): Empty {
            val stream = request.stream
            val key = request.key
            val streamCache = ignite.cache<Long, Any>(stream).withKeepBinary<Long, Any>()

            val queryChannel = Channel<Unit>(Channel.CONFLATED)
            val query = ContinuousQuery<Long, Any>()

            query.localListener = CacheEntryUpdatedListener { events ->
                try {
                    for (event in events) {
                        runBlocking { queryChannel.send(Unit) }
                    }
                } catch (e: Exception) {
                    queryChannel.close(e)
                }
            }

            query.remoteFilterFactory = Factory<CacheEntryEventFilter<Long, Any>> {
                CacheEntryEventFilter { event -> event.key == key }
            }

            val queryCursor = streamCache.query(query) // Important to start query before checking if it is already finished

            log.debug({ "Stream item notification listener started for $stream.'$key'!" })

            try {
                val item = streamCache.get(key)
                if (item == null)
                {
                    queryChannel.receive()
                }
                return Empty.getDefaultInstance()
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                log.error(e.toString())
                e.printStackTrace()
                throw e
            } finally {
                log.debug({ "Stream item notification listener completed for $stream.'$key'!" })
                queryCursor.close()
                queryChannel.close()
            }
        }
    }

    /*inner class ExternalScalerImpl : ExternalScalerGrpcKt.ExternalScalerCoroutineImplBase() {
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

        @OptIn(kotlinx.coroutines.FlowPreview::class)
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
    }*/
}
