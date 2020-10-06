package com.obecto.perper.fabric
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.QueryEntity
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.configuration.CacheConfiguration
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.ServiceResource
import org.apache.ignite.services.ServiceContext
import kotlin.collections.ArrayList

class StreamService : JobService() {
    val returnFieldName = "\$return"
    val forwardFieldName = "\$forward"

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    @set:ServiceResource(serviceName = "TransportService")
    lateinit var transportService: TransportService

    lateinit var streamsCache: IgniteCache<String, StreamData>
    lateinit var streamItemUpdates: Channel<Pair<String, Long>>

    var liveStreams = HashMap<String, StreamData>()
    var liveStreamGraph = HashMap<String, ArrayList<Pair<String, StreamData>>>()
    var liveWorkers = HashSet<String>()

    override fun init(ctx: ServiceContext) {
        streamItemUpdates = Channel(Channel.UNLIMITED)
        streamsCache = ignite.cache("streams")
        super.init(ctx)
    }

    override suspend fun executeJob(ctx: ServiceContext) {
        for ((stream, itemKey) in streamItemUpdates) {
            invokeStreamItemUpdates(stream, stream, itemKey)
        }
    }

    public fun engageStream(streamData: StreamData) {
        createCache(streamData)

        if (liveStreams.put(streamData.name, streamData) == null) {
            transportService.sendStreamTrigger(streamData.name, streamData.delegate)

            var allInputStreams = streamData.streamParams.mapValues({ it.value.map({ streamsCache.get(it) }) })
            for ((parameter, inputStreams) in allInputStreams) {
                for (inputStream in inputStreams) {
                    val list = liveStreamGraph.getOrPut(streamData.name, { ArrayList<Pair<String, StreamData>>() })
                    list.add(Pair(parameter, inputStream))
                    engageStream(inputStream)
                }
            }
        }
    }

    fun createCache(streamData: StreamData) {
        if (ignite.cacheNames().contains(streamData.name)) return

        val cache: IgniteCache<Long, BinaryObject> = if (streamData.indexType != null && streamData.indexFields != null) {
            val queryEntity = QueryEntity().also {
                it.keyType = Long::class.qualifiedName
                it.valueType = streamData.indexType
                it.fields = streamData.indexFields.associateTo(LinkedHashMap()) { it }
            }

            val cacheConfiguration = CacheConfiguration<Long, BinaryObject>(streamData.name).also {
                it.queryEntities = listOf(queryEntity)
            }
            ignite.createCache(cacheConfiguration).withKeepBinary()
        } else {
            ignite.createCache<Long, BinaryObject>(streamData.name).withKeepBinary()
        }

        // Consider alternative implementation
        if (ignite.atomicLong("${streamData.name}_Query", 0, true).compareAndSet(1, 0)) {
            val streamName = streamData.name
            val query = ContinuousQuery<Long, BinaryObject>()
            query.remoteFilterFactory = StreamServiceRemoteFilterFactory(streamName)

            // Dispose handle?
            cache.query(query)
        }
    }

    fun updateStream(streamData: StreamData) {
        var streamsCache = ignite.cache<String, StreamData>("streams")
        val previous = liveStreams.get(streamData.name) ?: return

        if (streamData.lastModified > previous.lastModified) {
            liveStreams[streamData.name] = streamData
            transportService.sendStreamTrigger(streamData.name, streamData.delegate)
        } else {
            val returnStreams = streamData.streamParams.get(returnFieldName) ?: emptyList()
            if (!returnStreams.isEmpty()) {
                for (name in returnStreams) {
                    val list = liveStreamGraph.getOrPut(streamData.name, { ArrayList<Pair<String, StreamData>>() })
                    if (!list.any({ it.first == forwardFieldName && it.second.name == streamData.name })) {
                        list.add(Pair(forwardFieldName, streamData))
                    }
                }
                var allReturnStreams = returnStreams.map({ streamsCache.get(it) })
                allReturnStreams.forEach({ engageStream(it) }) // TODO: Make parallel
            }

            if (!streamData.workers.isEmpty()) {
                for (worker in streamData.workers.values) {
                    if (worker.params.hasField(returnFieldName) && liveWorkers.contains(worker.name)) {
                        liveWorkers.remove(worker.name)
                        transportService.sendWorkerResult(streamData.name, worker.caller, worker.name)
                    } else if (!worker.params.hasField(returnFieldName) && !liveWorkers.contains(worker.name)) {
                        liveWorkers.add(worker.name)
                        transportService.sendWorkerTrigger(streamData.name, worker.caller, worker.name)
                    }
                }
            }
        }
    }

    fun updateStreamItem(stream: String, itemKey: Long) {
        runBlocking { streamItemUpdates.send(Pair(stream, itemKey)) }
    }

    fun invokeStreamItemUpdates(targetStream: String, stream: String, itemKey: Long) {
        val streamsToUpdate = liveStreamGraph[targetStream] ?: return
        for ((field, streamData) in streamsToUpdate) {
            if (field == forwardFieldName) {
                invokeStreamItemUpdates(streamData.name, stream, itemKey)
            } else {
                transportService.sendItemUpdate(streamData.name, streamData.delegate, field, stream, itemKey)
            }
        }
    }
}
