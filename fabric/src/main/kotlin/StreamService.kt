package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.StreamListener
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteException
import org.apache.ignite.IgniteLogger
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

object StreamServiceConstants {
    val persistAllIndex = Long.MIN_VALUE
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
        if (ignite.atomicLong("listeners-query", 0, true).compareAndSet(0, 1)) {
            val streamListenersCache = ignite.getOrCreateCache<Pair<String, String>, Long>("stream-listeners")
            log.debug({ "Starting stream listeners query" })

            val query = ContinuousQuery<Pair<String, String>, Long>()
            query.remoteFilterFactory = Factory<CacheEntryEventFilter<Pair<String, String>, Long>> { StreamListenerUpdatesRemoteFilter() }
            query.setAutoUnsubscribe(false)
            query.initialQuery = ScanQuery<Pair<String, String>, Long>()

            streamListenersCache.query(query)
        }
    }

    class StreamServiceHelpers(var ignite: Ignite, var log: IgniteLogger) {

        fun updateStreamListener(stream: String, listener: String, oldPosition: Long?, newPosition: Long?) {
            log.trace({ "Stream listener moved '$stream'.'$listener' $oldPosition->$newPosition" })
            if (newPosition != null)
            {
                ignite.atomicLong("$stream-at-$newPosition", 0, true).incrementAndGet()
            }

            if (oldPosition != null)
            {
                if (ignite.atomicLong("$stream-at-$oldPosition", 0, true).decrementAndGet() == 0L)
                {
                    cleanupStream(stream)
                }
            }
            else
            {
                startStreamQuery(stream)
            }
        }

        fun startStreamQuery(stream: String) {
            if (ignite.atomicLong("stream-$stream-query", 0, true).compareAndSet(0, 1)) {
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
                        if (ignite.atomicLong("$stream-at-$oldestElement", 0, true).get() == 0L) {
                            cache.remove(oldestElement)
                            keysQueue.remove(oldestElement)
                        }
                    }
                } finally {
                    updateLock.unlock();
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
                ignite.scheduler().runLocal(Runnable {
                    helpers.getKeysQueue(stream).put(event.key)
                })
            }
            return false
        }
    }

    class StreamListenerUpdatesRemoteFilter : CacheEntryEventFilter<Pair<String, String>, Long> {
        @set:IgniteInstanceResource
        lateinit var ignite: Ignite

        @set:LoggerResource
        lateinit var log: IgniteLogger

        val helpers by lazy(LazyThreadSafetyMode.PUBLICATION) { StreamServiceHelpers(ignite, log) }

        override fun evaluate(event: CacheEntryEvent<out Pair<String, String>, out Long>): Boolean {
            if (event.eventType == EventType.CREATED) {
                ignite.scheduler().runLocal(Runnable {
                    if (event.eventType == EventType.REMOVED || event.eventType == EventType.EXPIRED) {
                        helpers.updateStreamListener(event.key.first, event.key.second, event.value, null)
                    }
                    else
                    {
                        helpers.updateStreamListener(event.key.first, event.key.second, if (event.isOldValueAvailable) event.oldValue else null, event.value)
                    }
                })
            }
            return false
        }
    }
}
