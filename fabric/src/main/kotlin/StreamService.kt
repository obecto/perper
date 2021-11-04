package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.StreamListener
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.channels.SendChannel
import kotlinx.coroutines.channels.actor
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteLogger
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.configuration.CollectionConfiguration
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import java.util.concurrent.ConcurrentHashMap
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEvent
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.EventType
import kotlin.coroutines.EmptyCoroutineContext

object StreamServiceConstants {
    val persistAllIndex = Long.MIN_VALUE
}

object StreamServiceExecutor {
    val coroutineScope by lazy(LazyThreadSafetyMode.PUBLICATION) { CoroutineScope(EmptyCoroutineContext) }

    val channels = ConcurrentHashMap<String, SendChannel<Runnable>>()

    @OptIn(kotlinx.coroutines.ObsoleteCoroutinesApi::class)
    fun run(channelName: String, runnable: Runnable) {
        val channel = channels.getOrPut(channelName) {
            coroutineScope.actor<Runnable>(capacity = Channel.UNLIMITED) {
                for (_runnable in this) {
                    _runnable.run()
                }
            }
        }
        runBlocking {
            channel.send(runnable)
        }
    }
}

class StreamService : JobService() {
    lateinit var ignite: Ignite

    lateinit var log: IgniteLogger

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

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        val queryLock = ignite.reentrantLock("listeners-query", false, false, true)

        var couldLock = false
        try {
            couldLock = queryLock.tryLock()
        } catch (_: Exception) {}

        if (couldLock) {
            val streamListenersCache = ignite.getOrCreateCache<String, StreamListener>("stream-listeners")
            log.debug({ "Starting stream listeners query" })

            val query = ContinuousQuery<String, StreamListener>()
            query.remoteFilterFactory = Factory<CacheEntryEventFilter<String, StreamListener>> { StreamListenerUpdatesRemoteFilter() }
            query.setAutoUnsubscribe(false)
            query.initialQuery = ScanQuery<String, StreamListener>()

            streamListenersCache.query(query)
            // Not releasing the lock here; we only use to make sure only one setAutoUnsubscribe query is launched
        }
    }

    class StreamServiceHelpers(var ignite: Ignite, var log: IgniteLogger) {

        fun updateStreamListener(listener: String, stream: String, oldPosition: Long?, newPosition: Long?) {
            log.trace({ "Stream listener moved '$stream'.'$listener' $oldPosition->$newPosition" })
            if (newPosition != null) {
                ignite.atomicLong("$stream-at-$newPosition", 0, true).incrementAndGet()
            }

            if (oldPosition != null) {
                if (ignite.atomicLong("$stream-at-$oldPosition", 0, true).decrementAndGet() == 0L) {
                    cleanupStream(stream)
                }
            } else {
                startStreamQuery(stream)
            }
        }

        fun startStreamQuery(stream: String) {
            val queryLock = ignite.reentrantLock("stream-$stream-query", false, false, true)

            var couldLock = false
            try {
                couldLock = queryLock.tryLock()
            } catch (_: Exception) {}

            if (couldLock) {
                log.debug({ "Starting query on '$stream'" })

                val cache = ignite.cache<Long, Any>(stream).withKeepBinary<Long, BinaryObject>()

                val query = ContinuousQuery<Long, Any>()
                query.remoteFilterFactory = Factory<CacheEntryEventFilter<Long, Any>> { StreamItemUpdatesRemoteFilter(stream) }
                query.setAutoUnsubscribe(false)
                query.initialQuery = ScanQuery<Long, Any>()

                val queryCursor = cache.query(query)

                val itemKeys = queryCursor.map({ item -> item.key }).toMutableList() // NOTE: Sorts all keys in-memory; inefficient
                itemKeys.sort()
                getKeysQueue(stream).addAll(itemKeys)
            }
        }

        fun getKeysQueue(stream: String) = ignite.queue<Long>(
            "$stream-keys", 0,
            CollectionConfiguration().also {
                it.backups = 1 // Workaround IGNITE-7789
            }
        )

        fun cleanupStream(stream: String) {
            if (ignite.atomicLong("$stream-at-${StreamServiceConstants.persistAllIndex}", 0, true).get() > 0L) {
                return
            }

            val updateLock = ignite.reentrantLock("$stream-update", true, false, true)
            if (updateLock.tryLock()) { // If we fail to get the lock; don't worry, we will get to clean those up next time the minimum changes
                try {
                    val cache = ignite.cache<Long, Any>(stream).withKeepBinary<Long, BinaryObject>()
                    val keysQueue = getKeysQueue(stream)
                    while (true) {
                        val oldestElement = keysQueue.peek()
                        if (oldestElement == null) { // Queue is empty
                            break
                        }
                        val count = ignite.atomicLong("$stream-at-$oldestElement", 0, true).get()
                        log.trace({ "cleanup $stream; at $oldestElement = $count" })
                        if (count != 0L) {
                            break
                        }
                        cache.remove(oldestElement)
                        keysQueue.remove(oldestElement)
                    }
                } finally {
                    updateLock.unlock()
                }
            }
        }
    }

    class StreamItemUpdatesRemoteFilter(val stream: String) : CacheEntryEventFilter<Long, Any> {
        @set:IgniteInstanceResource
        lateinit var ignite: Ignite

        @set:LoggerResource
        lateinit var log: IgniteLogger

        val helpers by lazy(LazyThreadSafetyMode.PUBLICATION) { StreamServiceHelpers(ignite, log) }

        override fun evaluate(event: CacheEntryEvent<out Long, out Any>): Boolean {
            if (event.eventType == EventType.CREATED) {
                StreamServiceExecutor.run(
                    stream,
                    Runnable {
                        log.trace({ "add to $stream key ${event.key}" })
                        helpers.getKeysQueue(stream).put(event.key)
                    }
                )
            }
            return false
        }
    }

    class StreamListenerUpdatesRemoteFilter : CacheEntryEventFilter<String, StreamListener> {
        @set:IgniteInstanceResource
        lateinit var ignite: Ignite

        @set:LoggerResource
        lateinit var log: IgniteLogger

        val helpers by lazy(LazyThreadSafetyMode.PUBLICATION) { StreamServiceHelpers(ignite, log) }

        override fun evaluate(event: CacheEntryEvent<out String, out StreamListener>): Boolean {
            StreamServiceExecutor.run(
                event.key,
                Runnable {
                    if (event.eventType == EventType.REMOVED || event.eventType == EventType.EXPIRED) {
                        helpers.updateStreamListener(event.key, event.value.stream, event.value.position, null)
                    } else {
                        helpers.updateStreamListener(event.key, event.value.stream, if (event.isOldValueAvailable) event.oldValue.position else null, event.value.position)
                    }
                }
            )
            return false
        }
    }
}
