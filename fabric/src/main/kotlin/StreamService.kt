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
import org.apache.ignite.cache.affinity.AffinityKey
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.configuration.CacheConfiguration
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import javax.cache.CacheException
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener

class StreamService : JobService() {
    data class StreamOutput(val streamData: StreamData, val parameter: String, val filter: Map<List<String>, Any?>?, val localToData: Boolean)

    val returnFieldName = "\$return"
    val forwardFieldName = "\$forward"

    @set:LoggerResource
    lateinit var log: IgniteLogger

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    lateinit var streamsCache: IgniteCache<String, StreamData>
    lateinit var streamItemUpdates: Channel<Pair<String, Long>>

    var liveWorkers = HashSet<String>()

    override fun init(ctx: ServiceContext) {
        streamItemUpdates = Channel(Channel.UNLIMITED)
        streamsCache = ignite.getOrCreateCache("streams")

        super.init(ctx)
    }

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        launch {
            var streamGraphUpdates = Channel<Pair<String, StreamData>>(Channel.UNLIMITED)
            val query = ContinuousQuery<String, StreamData>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    runBlocking { streamGraphUpdates.send(Pair(event.key, event.value)) }
                }
            }
            streamsCache.query(query)
            log.debug({ "Streams listener started!" })
            for ((stream, streamData) in streamGraphUpdates) {
                log.debug({ "Stream object modified '$stream'" })
                updateStream(stream, streamData)
            }
        }
        launch {
            for ((stream, itemKey) in streamItemUpdates) {
                log.trace({ "Invoking stream updates on '$stream'" })
                val itemValue = lazy { ignite.cache<Long, BinaryObject>(stream).withKeepBinary<Long, BinaryObject>().get(itemKey) }
                invokeStreamItemUpdates(stream, stream, itemKey, itemValue)
            }
        }
    }

    suspend fun updateStream(stream: String, streamData: StreamData) {
        if (streamData.delegateType == StreamDelegateType.Action || streamData.listeners.size > 0) {
            engageStream(stream, streamData)
        }

        if (!streamData.workers.isEmpty()) {
            for (worker in streamData.workers.values) {
                if (worker.finished && liveWorkers.contains(worker.name)) {
                    liveWorkers.remove(worker.name)
                    val notificationsCache = getNotificationCache(stream)
                    val key = AffinityKey(System.currentTimeMillis(), stream)
                    notificationsCache.put(key, WorkerResultNotification(worker.name))
                } else if (!worker.finished && !liveWorkers.contains(worker.name)) {
                    liveWorkers.add(worker.name)
                    // FIXME
                    // transportService.sendWorkerTrigger(stream, worker.delegate, worker.name)
                }
            }
        }
    }

    public suspend fun engageStream(stream: String, streamData: StreamData) {
        if (createCache(stream, streamData)) {
            log.debug({ "Starting stream '$stream'" })
            val notificationsCache = getNotificationCache(stream)
            notificationsCache.put(AffinityKey(System.currentTimeMillis(), stream), StreamTriggerNotification())
        }
    }

    suspend fun createCache(stream: String, streamData: StreamData): Boolean {
        if (ignite.cacheNames().contains(stream)) return false
        log.debug({ "Creating cache '$stream'" })

        val cacheConfiguration = CacheConfiguration<Long, BinaryObject>(stream)

        if (streamData.indexType != null && streamData.indexFields != null) {
            log.debug({ "Cache for '$stream' uses indexing" })
            cacheConfiguration.queryEntities = listOf(
                QueryEntity().also {
                    it.keyType = Long::class.qualifiedName
                    it.valueType = streamData.indexType
                    it.fields = streamData.indexFields
                }
            )
        }

        try {
            var cache = ignite.createCache(cacheConfiguration).withKeepBinary<Long, BinaryObject>()
            ignite.getOrCreateCache<AffinityKey<Long>, StreamNotification>("$stream-\$notifications")

            // Consider alternative implementation
            if (ignite.atomicLong("${stream}_Query", 0, true).compareAndSet(0, 1)) {
                log.debug({ "Starting query on '$stream'" })
                val query = ContinuousQuery<Long, BinaryObject>()
                query.remoteFilterFactory = Factory<CacheEntryEventFilter<Long, BinaryObject>> { StreamServiceRemoteFilter(stream) }

                // Dispose handle?
                cache.query(query)
            }
        } catch (_: CacheException) {
            return false
        }

        return true
    }

    suspend fun invokeStreamItemUpdates(targetStream: String, cache: String, itemKey: Long, itemValue: Lazy<BinaryObject>) {
        val listeners = streamsCache.get(targetStream)?.listeners ?: return
        log.trace({ "Invoking stream updates for '$targetStream'; listeners: $listeners" })
        for (listener in listeners) {
            if (!testFilter(listener.filter, itemValue.value)) continue

            if (listener.parameter == forwardFieldName) {
                invokeStreamItemUpdates(listener.stream, cache, itemKey, itemValue)
            } else {
                val notificationsCache = getNotificationCache(listener.stream)
                val key = AffinityKey(itemKey, if (listener.localToData) listener.stream else itemKey)
                notificationsCache.put(key, StreamItemNotification(listener.parameter, cache, itemKey))
            }
        }
    }

    fun testFilter(filter: Map<String, Any?>, item: BinaryObject): Boolean {
        for ((field, expectedValue) in filter.entries) {
            val path = field.split('.')
            var finalItem: BinaryObject? = item
            for (segment in path.dropLast(1)) {
                if (finalItem != null && finalItem.hasField(segment)) {
                    finalItem = finalItem.field<BinaryObject?>(segment)
                } else {
                    finalItem = null
                    break
                }
            }
            if (expectedValue != finalItem?.field<Any?>(path.last())) {
                return false
            }
        }

        return true
    }

    fun getNotificationCache(stream: String) = ignite.cache<AffinityKey<Long>, StreamNotification>("$stream-\$notifications")

    fun updateStreamItem(stream: String, itemKey: Long) {
        log.trace({ "Queueing stream update on '$stream'" })
        runBlocking { streamItemUpdates.send(Pair(stream, itemKey)) }
    }
}
