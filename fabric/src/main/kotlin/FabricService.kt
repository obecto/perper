package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.AgentType
import com.obecto.perper.fabric.cache.InstanceData
import com.obecto.perper.fabric.cache.ExecutionData
import com.obecto.perper.fabric.cache.StreamListener
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.ExecutionsRequest
import com.obecto.perper.protobuf.ExecutionFinishedRequest
import com.obecto.perper.protobuf.ListenerAttachedRequest
import com.obecto.perper.protobuf.ExecutionsResponse
import com.obecto.perper.protobuf.StreamItemsRequest
import com.obecto.perper.protobuf.StreamItemsResponse
import io.grpc.Server
import io.grpc.ServerBuilder
import kotlinx.coroutines.GlobalScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.channels.ProducerScope
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.flow.buffer
import kotlinx.coroutines.flow.channelFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.Flow
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
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicInteger
import javax.cache.Cache.Entry
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEvent
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener
import javax.cache.event.EventType
import com.google.protobuf.Empty

private inline val EventType.isRemoval get() = this == EventType.REMOVED || this == EventType.EXPIRED
private inline val EventType.isCreation get() = this == EventType.CREATED

class FabricService(var port: Int = 40400) : Service {

    /*companion object Ticks {
        val startTicks = (System.currentTimeMillis()) * 10_000
        val startNanos = System.nanoTime()

        fun getCurrentTicks() = startTicks + (System.nanoTime() - startNanos) / 100
    }*/

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

    @OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
    private fun <T> tryCatchChannelFlow(block: suspend ProducerScope<T>.() -> Unit): Flow<T> = channelFlow<T> {
        try {
            block()
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            log.error(e.toString())
            e.printStackTrace()
            throw e
        }
    }

    private fun <T, K, V> ContinuousQuery<K, V>.setChannelLocalListener (channel: Channel<T>, block: suspend Channel<T>.(CacheEntryEvent<out K, out V>) -> Unit): Channel<T> {
        this.localListener = CacheEntryUpdatedListener { events ->
            try {
                for (event in events) {
                    runBlocking { channel.block(event) }
                }
            } catch (e: Exception) {
                channel.close(e)
            }
        }
        return channel
    }

    inner class FabricImpl : FabricGrpcKt.FabricCoroutineImplBase() {

        @OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
        override fun executions(request: ExecutionsRequest) = tryCatchChannelFlow<ExecutionsResponse> {
            val agent = request.agent
            val instance = if (request.instance != "") request.instance else null
            val localToData = request.localToData

            InstanceService.setAgentType(ignite, agent, if (instance == null) AgentType.FUNCTIONS else AgentType.CONTAINERS)

            val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

            val query = ContinuousQuery<String, ExecutionData>()
            val queryChannel = query.setChannelLocalListener(Channel<Pair<String, Pair<Boolean, ExecutionData>>>(Channel.UNLIMITED)) {
                event -> send(Pair(event.key, Pair(event.eventType.isRemoval, event.value)))
            }
            query.setLocal(localToData)

            query.remoteFilterFactory = Factory { CacheEntryEventFilter { event ->
                if (event.isOldValueAvailable && !event.oldValue.finished)
                    false
                else // NOTE: Can probably use a single query for all executions
                    event.value.agent == agent && (instance == null || event.value.instance == instance) && !event.value.finished
            } }
            query.initialQuery = ScanQuery<String, ExecutionData>().also {
                it.setLocal(localToData)
                it.filter = IgniteBiPredicate<String, ExecutionData> { _, value ->
                    value.agent == agent && (instance == null || value.instance == instance) && !value.finished
                }
            }

            log.debug({ "Executions listener started for '$agent'-'$instance'!" })

            val queryCursor = executionsCache.query(query)

            try {
                val sentExecutions = HashMap<String, Boolean>()

                suspend fun ProducerScope<ExecutionsResponse>.output(key: String, value: ExecutionData, removed: Boolean) {
                    if (sentExecutions.put(key, removed) != removed) {
                        send(ExecutionsResponse.newBuilder().also {
                            it.instance = value.instance
                            it.delegate = value.delegate
                            it.execution = key
                            it.cancelled = removed
                        }.build())
                    }
                }

                for (entry in queryCursor) {
                    output(entry.key, entry.value, false)
                }

                for (pair in queryChannel) {
                    output(pair.first, pair.second.second, pair.second.first)
                }
            } catch (e: CancellationException) {
                throw e
            } catch (e: Exception) {
                log.error(e.toString())
                e.printStackTrace()
                throw e
            } finally {
                log.debug({ "Executions listener stopped for '$agent'-'$instance'!" })
                queryCursor.close()
            }
        }.buffer(Channel.UNLIMITED)

        override suspend fun executionFinished(request: ExecutionFinishedRequest): Empty {
            val execution = request.execution

            val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

            val query = ContinuousQuery<String, ExecutionData>()
            val queryChannel = query.setChannelLocalListener(Channel<Unit>(Channel.CONFLATED)) { _ -> send(Unit) }

            query.remoteFilterFactory = Factory { CacheEntryEventFilter {
                event -> event.eventType.isRemoval || (event.key == execution && event.value.finished)
            } }

            val queryCursor = executionsCache.query(query) // Important to start query before checking if it is already finished

            log.debug({ "Execution finished listener started for '$execution'!" })

            try {
                val executionData = executionsCache.get(execution)
                if (executionData == null || !executionData.finished)
                {
                    queryChannel.receive()
                }
                return Empty.getDefaultInstance()
            } finally {
                log.debug({ "Execution finished listener completed for '$execution'!" })
                queryCursor.close()
                queryChannel.close()
            }
        }

        @OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
        override fun streamItems(request: StreamItemsRequest) = tryCatchChannelFlow<StreamItemsResponse> {
            val stream = request.stream
            val startKey = if (request.startKey != -1L) request.startKey else null
            val stride = request.stride
            val localToData = request.localToData

            suspend fun ProducerScope<StreamItemsResponse>.output(key: Long) {
                send(StreamItemsResponse.newBuilder().also {
                    it.key = key
                }.build())
            }

            val streamCache = ignite.cache<Long, Any>(stream).withKeepBinary<Long, Any>()

            if (stride == 0L)
            {
                val query = ContinuousQuery<Long, Any>()
                val queryChannel = query.setChannelLocalListener(Channel<Long>(Channel.UNLIMITED)) { event -> send(event.key) }
                query.setLocal(localToData)

                if (startKey == null)
                {
                    query.remoteFilterFactory = Factory { CacheEntryEventFilter {
                        event -> event.eventType.isCreation
                    } }
                    //query.initialQuery = null;
                }
                else
                {
                    query.remoteFilterFactory = Factory { CacheEntryEventFilter {
                        event -> event.eventType.isCreation && event.key > startKey
                    } }
                    query.initialQuery = ScanQuery(IgniteBiPredicate<Long, Any> { key, _ -> key > startKey }).also {
                        it.setLocal(localToData)
                    }
                }

                val queryCursor = streamCache.query(query)
                log.debug({ "Stream items listener started for '${stream}' ${startKey ?: "last"}..!" })

                try {
                    val itemKeys = queryCursor.map({ item -> item.key }).toMutableList() // NOTE: Sorts all keys in-memory; inefficient
                    itemKeys.sort()

                    for (key in itemKeys) {
                        output(key)
                    }
                    var lastKey = itemKeys.lastOrNull() ?: startKey

                    for (key in queryChannel) {
                        if (lastKey == null || key > lastKey)
                        {
                            lastKey = key
                            output(key)
                        }
                    }
                } finally {
                    queryCursor.close()
                    log.debug({ "Stream items listener finished for '${stream}' ${startKey ?: "last"}..!" })
                }
            }
            else // if (stride != 0L)
            {
                log.debug({ "Stream items listener started for '${stream}' ${startKey ?: "last"}..+${stride}!" })
                try {
                    var keyOrNull = startKey

                    if (keyOrNull == null) {
                        val keys = streamCache.query(ScanQuery<Long, Any>().also {
                            it.setLocal(localToData)
                        }).map({ item -> item.key })
                        keyOrNull = if (stride > 0) keys.maxOrNull() else keys.minOrNull()
                    }

                    if (keyOrNull == null) { // Cache is empty
                        val query = ContinuousQuery<Long, Any>()
                        val queryChannel = query.setChannelLocalListener(Channel<Long>(1, BufferOverflow.DROP_LATEST)) { event -> send(event.key) }
                        val queryCursor = streamCache.query(query)
                        try {
                            keyOrNull = queryChannel.receive()
                        } finally {
                            queryCursor.close()
                            queryChannel.close()
                        }
                    }

                    var key = keyOrNull!!

                    while (true) {
                        if (!streamCache.containsKey(key))
                        {
                            val query = ContinuousQuery<Long, Any>()
                            val queryChannel = query.setChannelLocalListener(Channel<Unit>(Channel.CONFLATED)) { _ -> send(Unit) }

                            query.remoteFilterFactory = Factory { CacheEntryEventFilter {
                                event -> event.key == key
                            } }

                            val queryCursor = streamCache.query(query) // Important to start query before checking if it is already finished

                            try {
                                if (!streamCache.containsKey(key)) // Race check
                                {
                                    queryChannel.receive()
                                }
                            } finally {
                                queryCursor.close()
                                queryChannel.close()
                            }
                        }

                        output(key)

                        key += stride
                    }
                } finally {
                    log.debug({ "Stream items listener finished for '${stream}' ${startKey ?: "last"}..+${stride}!" })
                }
            }
        }.buffer(Channel.UNLIMITED)

        override suspend fun listenerAttached(request: ListenerAttachedRequest): Empty {
            val stream = request.stream

            val streamListenersCacheName = "stream-listeners"
            val streamListenersCache = ignite.cache<String, StreamListener>(streamListenersCacheName)

            val query = ContinuousQuery<String, StreamListener>()
            val queryChannel = query.setChannelLocalListener(Channel<Unit>(Channel.CONFLATED)) { _ -> send(Unit) }

            query.remoteFilterFactory = Factory { CacheEntryEventFilter {
                event -> event.eventType.isCreation  && event.value.stream == stream
            } }
            query.initialQuery = ScanQuery(IgniteBiPredicate<String, StreamListener> { _, value -> value.stream == stream })

            val queryCursor = streamListenersCache.query(query)

            log.debug({ "Listener attached listener started for '$stream'!" })

            try {
                for (_i in queryCursor) {
                    return Empty.getDefaultInstance()
                }
                queryChannel.receive()
                return Empty.getDefaultInstance()
            } finally {
                log.debug({ "Listener attached listener completed for '$stream'!" })
                queryCursor.close()
                queryChannel.close()
            }
        }
    }

    inner class ExternalScalerImpl : ExternalScalerGrpcKt.ExternalScalerCoroutineImplBase() {
        val ScaledObjectRef.agent
            get() = scalerMetadataMap.getOrDefault("agent", name)
        //val ScaledObjectRef.targetExecutions
        //    get() = scalerMetadataMap.getOrDefault("targetExecutions", "10").toLong()
        val ScaledObjectRef.targetInstances
            get() = scalerMetadataMap.getOrDefault("targetInstances", "10").toLong()

        override suspend fun isActive(request: ScaledObjectRef): IsActiveResponse {
            val agent = request.agent

            val instancesCache = ignite.getOrCreateCache<String, InstanceData>("instances")
            val query = ScanQuery(IgniteBiPredicate<String, InstanceData> {_, value -> value.agent == agent}).also {
                it.setLocal(true)
            }
            val queryCursor = instancesCache.query(query)
            val hasAvailable = queryCursor.any()
            return IsActiveResponse.newBuilder().also {
                it.result = hasAvailable
            }.build()
        }

        @OptIn(kotlinx.coroutines.FlowPreview::class)
        override fun streamIsActive(request: ScaledObjectRef) = flow<Boolean> {
            val agent = request.agent

            val instancesCache = ignite.getOrCreateCache<String, InstanceData>("instances")

            var count = AtomicInteger(0)
            lateinit var queryCursor: QueryCursor<Entry<String, InstanceData>>

            val query = ContinuousQuery<String, InstanceData>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    if (event.value.agent == agent) {
                        val value = if (event.eventType.isRemoval) count.incrementAndGet() else if (event.eventType.isCreation) count.decrementAndGet() else continue
                        runBlocking {
                            try {
                                emit(value > 0)
                            } catch (_: CancellationException) {
                                queryCursor.close()
                            }
                        }
                    }
                }
            }
            query.setLocal(true)
            query.initialQuery = ScanQuery(IgniteBiPredicate<String, InstanceData> {_, value -> value.agent == agent}).also {
                it.setLocal(true)
            }
            queryCursor = instancesCache.query(query)

            for (entry in queryCursor) {
                emit(count.incrementAndGet() > 0)
            }
        }.debounce(100).map({ value ->
            IsActiveResponse.newBuilder().also {
                it.result = value
            }.build()
        })

        override suspend fun getMetricSpec(request: ScaledObjectRef): GetMetricSpecResponse {
            val agent = request.agent
            return GetMetricSpecResponse.newBuilder().also {
                it.addMetricSpecsBuilder().also {
                    it.metricName = "$agent-instances"
                    it.targetSize = request.targetInstances
                }
            }.build()
        }

        override suspend fun getMetrics(request: GetMetricsRequest): GetMetricsResponse {
            val agent = request.scaledObjectRef.agent

            val instancesCache = ignite.getOrCreateCache<String, InstanceData>("instances")

            val query = ScanQuery(IgniteBiPredicate<String, InstanceData> {_, value -> value.agent == agent}).also {
                it.setLocal(true)
            }
            val queryCursor = instancesCache.query(query)
            val available = queryCursor.count()

            return GetMetricsResponse.newBuilder().also {
                it.addMetricValuesBuilder().also {
                    it.metricName = "$agent-instances"
                    it.metricValue = available.toLong()
                }
            }.build()
        }
    }
}
