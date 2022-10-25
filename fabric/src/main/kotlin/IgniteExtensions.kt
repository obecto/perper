package com.obecto.perper.fabric
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.emitAll
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withContext
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteInterruptedException
import org.apache.ignite.IgniteLogger
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.lang.IgniteBiPredicate
import org.apache.ignite.lang.IgniteFuture
import org.apache.ignite.lang.IgniteInClosure
import java.lang.InterruptedException
import java.lang.Thread
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEvent
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener
import javax.cache.event.EventType
import kotlin.concurrent.thread

internal inline fun IgniteLogger.info(f: () -> String) {
    if (isInfoEnabled()) info(f())
}

internal inline fun IgniteLogger.debug(f: () -> String) {
    if (isDebugEnabled()) debug(f())
}

internal inline fun IgniteLogger.trace(f: () -> String) {
    if (isTraceEnabled()) trace(f())
}

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
internal suspend fun <V> IgniteFuture<V>.await() = suspendCancellableCoroutine<V> { continuation -> // via https://stackoverflow.com/a/67637691
    listen(
        IgniteInClosure { result ->
            continuation.resume(result.get()) {}
        }
    )
    continuation.invokeOnCancellation {
        cancel()
    }
}

internal inline val EventType.isRemoval get() = this == EventType.REMOVED || this == EventType.EXPIRED

internal inline val EventType.isCreation get() = this == EventType.CREATED

internal fun <T, K, V> ContinuousQuery<K, V>.setChannelLocalListener(channel: Channel<T>, block: suspend Channel<T>.(CacheEntryEvent<out K, out V>) -> Unit): Channel<T> {
    this.localListener = CacheEntryUpdatedListener { events ->
        try {
            runBlocking {
                for (event in events) {
                    channel.block(event)
                }
            }
        } catch (e: Exception) {
            channel.close(e)
        }
    }
    return channel
}

internal inline fun <K, V> ContinuousQuery<K, V>.setScanAndRemoteFilter(crossinline block: (K, V?, V?) -> Boolean) {
    this.remoteFilterFactory = Factory {
        CacheEntryEventFilter { event ->
            val value = when (event.eventType!!) {
                EventType.CREATED -> event.value
                EventType.UPDATED -> event.value
                EventType.REMOVED, EventType.EXPIRED -> null
            }
            val oldValue = when (event.eventType!!) {
                EventType.CREATED -> null
                EventType.UPDATED -> event.oldValue
                EventType.REMOVED, EventType.EXPIRED -> event.value
            }
            block(event.key, value, oldValue)
        }
    }
    this.initialQuery = ScanQuery<K, V>().also {
        it.setLocal(this.isLocal())
        it.filter = IgniteBiPredicate<K, V> { key, value -> block(key, value, null) }
    }
}

internal fun <K, V> IgniteCache<K, V>.iterateQuery(cquery: ContinuousQuery<K, V>) = flow<Pair<K, V?>?> {

    val queryChannel = cquery.setChannelLocalListener(Channel<Pair<K, V?>>(Channel.UNLIMITED)) { event ->
        send(
            Pair(
                event.key,
                when (event.eventType!!) {
                    EventType.CREATED, EventType.UPDATED -> event.value
                    EventType.REMOVED, EventType.EXPIRED -> null
                }
            )
        )
    }

    val queryCursor = query(cquery)
    try {
        for (entry in queryCursor) {
            emit(Pair(entry.key, entry.value))
        }

        emit(null)

        emitAll(queryChannel)
    } finally {
        queryCursor.close()
        queryChannel.close()
    }
}

internal suspend fun <K, V> IgniteCache<K, V>.optimisticUpdateSuspend(key: K, block: suspend (V?) -> V) {
    while (true) {
        val value = this.getAsync(key).await()
        val newValue = block(value)
        if (this.replaceAsync(key, value, newValue).await()) {
            break
        }
    }
}

internal suspend fun Ignite.withLockSuspend(name: String, fastLockFailBlock: suspend () -> Unit = {}, block: suspend () -> Unit) {
    val locked = Channel<Boolean>(Channel.CONFLATED) // Expected: either a single `true` or a `false` followed by a `true`

    var finished = false
    val lock = Object()

    lateinit var lockThread: Thread
    lockThread = thread { // Sigh. Ignite locks are thread-bound and don't care about async usage
        try {
            val executionLock = reentrantLock(name, true, false, true)
            val tryLockSuccess = executionLock.tryLock()
            if (!tryLockSuccess) {
                runBlocking {
                    locked.send(false)
                }
                executionLock.lockInterruptibly()
            }
            runBlocking {
                locked.send(true)
            }
            try {
                synchronized(lock) {
                    while (!finished)
                        lock.wait()
                }
            } finally {
                Thread.interrupted() // Clear interrupted status so that unlock doesn't think we've been interrupted
                executionLock.unlock()
            }
        } catch (_: IgniteInterruptedException) {
        } catch (_: InterruptedException) {
        }
    }

    try {
        if (locked.receive()) {
            block()
        } else {
            fastLockFailBlock()
            assert(locked.receive())
            block()
        }
    } finally {
        withContext(Dispatchers.IO + NonCancellable) {
            synchronized(lock) {
                finished = true
                lock.notifyAll()
            }
            lockThread.interrupt()
            lockThread.join()
        }
    }
}
