package com.obecto.perper.fabric
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.QueryEntity
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.configuration.CacheConfiguration
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.resources.ServiceResource
import org.apache.ignite.services.ServiceContext
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener
import kotlin.collections.ArrayList

class StreamService : JobService() {
    val returnFieldName = "\$return"
    val forwardFieldName = "\$forward"

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    @set:ServiceResource(serviceName = "TransportService")
    lateinit var transportService: TransportService

    @set:LoggerResource
    lateinit var log: IgniteLogger

    lateinit var streamsCache: IgniteCache<String, StreamData>
    lateinit var streamItemUpdates: Channel<Pair<String, Long>>

    var liveStreams = HashMap<String, StreamData>()
    var liveStreamGraph = HashMap<String, ArrayList<Pair<String, StreamData>>>() // producer => [(parameter, consumer)]
    var liveWorkers = HashSet<String>()

    override fun init(ctx: ServiceContext) {
        streamItemUpdates = Channel(Channel.UNLIMITED)
        streamsCache = ignite.getOrCreateCache("streams")

        super.init(ctx)
    }

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        launch {
            var updates = Channel<StreamData>(Channel.UNLIMITED)
            val query = ContinuousQuery<String, StreamData>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    runBlocking { updates.send(event.value) }
                }
            }
            streamsCache.query(query)

            log.debug({ "Streams listener started!" })
            for (update in updates) {
                log.debug({ "Stream object modified '${update.name}'" })
                updateStream(update)
            }
        }

        launch {
            for ((stream, itemKey) in streamItemUpdates) {
                log.trace({ "Invoking stream updates on '$stream'" })
                invokeStreamItemUpdates(stream, stream, itemKey)
            }
        }
    }

    public suspend fun engageStream(streamData: StreamData) {
        createCache(streamData)

        if (liveStreams.put(streamData.name, streamData) == null) {
            log.debug({ "Starting stream '${streamData.name}'" })
            transportService.sendStreamTrigger(streamData.name, streamData.delegate)

            var allInputStreams = streamData.streamParams.mapValues({ it.value.map({ streamsCache.get(it) }) })
            for ((parameter, inputStreams) in allInputStreams) {
                for (inputStream in inputStreams) {
                    val list = liveStreamGraph.getOrPut(inputStream.name, { ArrayList<Pair<String, StreamData>>() })
                    list.add(Pair(parameter, streamData))
                    log.debug({ "Subscribing '${streamData.name}' to '${inputStream.name}'" })
                    engageStream(inputStream)
                }
            }
        }
    }

    suspend fun createCache(streamData: StreamData) {
        if (ignite.cacheNames().contains(streamData.name)) return
        log.debug({ "Creating cache '${streamData.name}'" })

        val cache: IgniteCache<Long, BinaryObject> = if (streamData.indexType != null && streamData.indexFields != null) {
            log.debug({ "Cache for '${streamData.name}' uses indexing" })
            val queryEntity = QueryEntity().also {
                it.keyType = Long::class.qualifiedName
                it.valueType = streamData.indexType
                it.fields = streamData.indexFields
            }

            val cacheConfiguration = CacheConfiguration<Long, BinaryObject>(streamData.name).also {
                it.queryEntities = listOf(queryEntity)
            }
            ignite.createCache(cacheConfiguration).withKeepBinary()
        } else {
            ignite.createCache<Long, BinaryObject>(streamData.name).withKeepBinary()
        }

        // Consider alternative implementation
        if (ignite.atomicLong("${streamData.name}_Query", 0, true).compareAndSet(0, 1)) {
            log.debug({ "Starting query on '${streamData.name}'" })
            val streamName = streamData.name
            val query = ContinuousQuery<Long, BinaryObject>()
            query.remoteFilterFactory = Factory<CacheEntryEventFilter<Long, BinaryObject>> { StreamServiceRemoteFilter(streamName) }

            // Dispose handle?
            cache.query(query)
        }
    }

    suspend fun updateStream(streamData: StreamData) {
        if (streamData.delegateType == StreamDelegateType.Action) {
            engageStream(streamData)
        }

        val previous = liveStreams.get(streamData.name) ?: return

        if (streamData.lastModified > previous.lastModified) {
            log.debug({ "Restarting stream '${streamData.name}'" })
            liveStreams[streamData.name] = streamData
            transportService.sendStreamTrigger(streamData.name, streamData.delegate)
        } else {
            val returnStreams = streamData.streamParams.get(returnFieldName) ?: emptyList()
            if (!returnStreams.isEmpty()) {
                for (name in returnStreams) {
                    val list = liveStreamGraph.getOrPut(name, { ArrayList<Pair<String, StreamData>>() })
                    if (!list.any({ it.first == forwardFieldName && it.second.name == streamData.name })) {
                        list.add(Pair(forwardFieldName, streamData))
                    }
                }
                var allReturnStreams = returnStreams.map({ streamsCache.get(it) })
                allReturnStreams.forEach({ engageStream(it) }) // TODO: Make parallel
            }

            if (!streamData.workers.isEmpty()) {
                for (worker in streamData.workers.values) {
                    if ((worker.params?.hasField(returnFieldName) ?: false) && liveWorkers.contains(worker.name)) {
                        liveWorkers.remove(worker.name)
                        transportService.sendWorkerResult(streamData.name, worker.caller, worker.name)
                    } else if (!(worker.params?.hasField(returnFieldName) ?: false) && !liveWorkers.contains(worker.name)) {
                        liveWorkers.add(worker.name)
                        transportService.sendWorkerTrigger(streamData.name, worker.delegate, worker.name)
                    }
                }
            }
        }
    }

    suspend fun invokeStreamItemUpdates(targetStream: String, stream: String, itemKey: Long) {
        val streamsToUpdate = liveStreamGraph[targetStream] ?: return
        log.trace({ "Invoking stream updates for '$targetStream'; listeners: $streamsToUpdate" })
        for ((field, streamData) in streamsToUpdate) {
            if (field == forwardFieldName) {
                invokeStreamItemUpdates(streamData.name, stream, itemKey)
            } else {
                transportService.sendItemUpdate(streamData.name, streamData.delegate, field, stream, itemKey)
            }
        }
    }

    fun updateStreamItem(stream: String, itemKey: Long) {
        log.trace({ "Queueing stream update on '$stream'" })
        runBlocking { streamItemUpdates.send(Pair(stream, itemKey)) }
    }
}
