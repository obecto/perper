/*package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.StreamData
import com.obecto.perper.fabric.cache.StreamDelegateType
import com.obecto.perper.fabric.cache.StreamListener
import com.obecto.perper.fabric.cache.notification.NotificationKey
import com.obecto.perper.fabric.cache.notification.StreamItemNotification
import com.obecto.perper.fabric.cache.notification.StreamTriggerNotification
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteException
import org.apache.ignite.IgniteLogger
import org.apache.ignite.IgniteSet
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.QueryEntity
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.configuration.CacheConfiguration
import org.apache.ignite.configuration.CollectionConfiguration
import org.apache.ignite.lang.IgniteBiPredicate
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import javax.cache.CacheException
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEvent
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener
import javax.cache.event.EventType

class StreamService : JobService() {
    val forwardParameterIndex = Int.MIN_VALUE

    lateinit var log: IgniteLogger

    lateinit var ignite: Ignite

    lateinit var streamsCache: IgniteCache<String, StreamData>
    lateinit var replayStartedSet: IgniteSet<Pair<String, StreamListener>>
    lateinit var streamItemUpdates: Channel<Pair<String, Long>>

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
        streamItemUpdates = Channel(Channel.UNLIMITED)
        streamsCache = ignite.getOrCreateCache("streams")
        replayStartedSet = ignite.set(
            "\$replays-started",
            CollectionConfiguration().also {
                it.backups = 1
            }
        )

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
                invokeStreamItemUpdates(stream, itemKey)
            }
        }
    }

    suspend fun updateStream(stream: String, streamData: StreamData) {
        when (streamData.delegateType) {
            StreamDelegateType.Action -> engageStream(stream, streamData)
            StreamDelegateType.Function -> if (streamData.listeners.size > 0) engageStream(stream, streamData)
            StreamDelegateType.External -> createCache(stream, streamData)
        }

        for (listener in streamData.listeners) {
            if (listener.replay) {
                if (replayStartedSet.add(Pair(stream, listener))) {
                    writeFullReplay(stream, streamData.ephemeral, listener)
                }
            }
        }
    }

    suspend fun engageStream(stream: String, streamData: StreamData) {
        if (createCache(stream, streamData)) {
            log.debug({ "Starting stream '$stream'" })
            val notificationsCache = TransportService.getNotificationCache(ignite, streamData.agent)
            notificationsCache.put(NotificationKey(TransportService.getCurrentTicks(), stream), StreamTriggerNotification(stream, streamData.delegate))
        }
    }

    suspend fun createCache(stream: String, streamData: StreamData): Boolean {
        if (ignite.cacheNames().contains(stream)) return false
        log.debug({ "Creating cache '$stream'" })

        val cacheConfiguration = CacheConfiguration<Long, Any>(stream)

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
            var cache = ignite.createCache(cacheConfiguration).withKeepBinary<Long, Any>()

            // Consider alternative implementation
            if (ignite.atomicLong("${stream}_Query", 0, true).compareAndSet(0, 1)) {
                log.debug({ "Starting query on '$stream'" })
                val query = ContinuousQuery<Long, Any>()
                query.remoteFilterFactory = Factory<CacheEntryEventFilter<Long, Any>> { StreamUpdatesRemoteFilter(stream) }
                query.setAutoUnsubscribe(false)
                cache.query(query)
            }
        } catch (e: CacheException) {
            log.error("Unexpected error when creating cache for stream '$stream': $e")
            e.printStackTrace()
            return false
        }

        return true
    }

    suspend fun invokeStreamItemUpdates(stream: String, itemKey: Long) {
        val itemValue = lazy { ignite.cache<Long, Any>(stream).withKeepBinary<Long, BinaryObject>().get(itemKey) }
        val ephemeral = streamsCache.get(stream).ephemeral
        var ephemeralCounter = 0L
        val notificationKey = TransportService.getCurrentTicks()

        suspend fun helper(targetStream: String) {
            val listeners = streamsCache.get(targetStream)?.listeners ?: return
            log.trace({ "Invoking stream updates for '$targetStream'; listeners: $listeners" })
            for (listener in listeners) {
                if (!testFilter(listener.filter, itemValue)) continue

                if (listener.parameter == forwardParameterIndex) {
                    helper(listener.stream)
                } else {
                    ephemeralCounter ++
                    val notificationsCache = TransportService.getNotificationCache(ignite, listener.agent)
                    val notificationsQueue = TransportService.getNotificationQueue(ignite, listener.stream)
                    val key = NotificationKey(notificationKey, if (listener.localToData) itemKey else listener.stream)
                    notificationsQueue.put(key)
                    notificationsCache.put(key, StreamItemNotification(listener.stream, listener.parameter, stream, itemKey, ephemeral))
                    log.trace({ "Writing notification: ${listener.stream} - ${key} (${notificationsQueue.size})" })
                }
            }
        }
        helper(stream)

        if (ephemeral) {
            val counter = ignite.atomicLong("$stream-$itemKey", 0, true)
            if (counter.addAndGet(ephemeralCounter) == 0L) {
                // Special case when all notifications are consumed before we finish invoking updates (or when no listeners are present)
                ignite.cache<Long, Any>(stream).withKeepBinary<Long, Any>().remove(itemKey)
                counter.close()
            }
        }
    }

    fun testFilter(filter: Map<String, Any?>, item: Lazy<BinaryObject>): Boolean {
        for ((field, expectedValue) in filter.entries) {
            val path = field.split('.')
            var finalItem: BinaryObject? = item.value
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

    fun updateStreamItem(stream: String, itemKey: Long) {
        log.trace({ "Queueing stream update on '$stream'" })
        runBlocking { streamItemUpdates.send(Pair(stream, itemKey)) }
    }

    fun writeFullReplay(stream: String, ephemeral: Boolean, listener: StreamListener) {
        val cache = ignite.cache<Long, Any>(stream).withKeepBinary<Long, Any>()
        val notificationsCache = TransportService.getNotificationCache(ignite, listener.agent)
        val notificationsQueue = TransportService.getNotificationQueue(ignite, listener.stream)

        val query = ScanQuery<Long, Any>()
        query.filter = IgniteBiPredicate { _, value ->
            testFilter(listener.filter, lazy { value as BinaryObject })
        }

        var itemKeys = cache.query(query).map({ item -> item.key }).toMutableList()
        itemKeys.sort()

        for (itemKey in itemKeys) {
            if (ephemeral) {
                try {
                    val counter = ignite.atomicLong("$stream-$itemKey", 0, false)
                    if (counter.getAndIncrement() == 0L) {
                        continue; // If it was 0, someone is going to delete the item very soon
                    }
                    if (!cache.containsKey(itemKey)) {
                        continue; // In case we somehow raced and the key is gone anyway
                    }
                    // Can still race if we enter writeFullReplace twice for the same stream?
                } catch (e: IgniteException) {
                    continue
                }
            }

            val key = NotificationKey(TransportService.getCurrentTicks(), if (listener.localToData) itemKey else listener.stream)
            notificationsQueue.put(key)
            notificationsCache.put(key, StreamItemNotification(listener.stream, listener.parameter, stream, itemKey, ephemeral))
            log.trace({"Writing notification (replay): ${listener.stream} - ${key} (${notificationsQueue.size})"})
        }
    }

    class StreamUpdatesRemoteFilter(val streamName: String) : CacheEntryEventFilter<Long, Any> {
        @set:IgniteInstanceResource
        lateinit var ignite: Ignite

        override fun evaluate(event: CacheEntryEvent<out Long, out Any>): Boolean {
            if (event.eventType == EventType.CREATED) {
                var service = ignite.services().service<StreamService>("StreamService")
                service.updateStreamItem(streamName, event.key)
            }
            return false
        }
    }
}
*/
