package com.obecto.perper.fabric
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.channels.BufferOverflow
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
import org.apache.ignite.cache.CachePeekMode
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
internal suspend fun <V> IgniteFuture<V>.await() = suspendCancellableCoroutine<V?> { continuation -> // via https://stackoverflow.com/a/67637691
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

internal inline val <K, V> CacheEntryEvent<K, V>.currentValue get() = when (this.eventType!!) {
    EventType.CREATED, EventType.UPDATED -> this.value
    EventType.REMOVED, EventType.EXPIRED -> null
}

internal inline val <K, V> CacheEntryEvent<K, V>.oldValue get() = when (this.eventType!!) {
    EventType.CREATED -> null
    EventType.UPDATED -> this.oldValue
    EventType.REMOVED, EventType.EXPIRED -> this.value
}

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
            block(event.key, event.currentValue, event.oldValue)
        }
    }
    this.initialQuery = ScanQuery<K, V>().also {
        it.setLocal(this.isLocal())
        it.filter = IgniteBiPredicate<K, V> { key, value -> block(key, value, null) }
    }
}

internal fun <K, V> IgniteCache<K, V>.iterateQuery(cquery: ContinuousQuery<K, V>, channelCapacity: Int = Channel.UNLIMITED, onBufferOverflow: BufferOverflow = BufferOverflow.SUSPEND, signalStartOfStream: suspend () -> Unit = {}, sortKeysBy: Comparator<in K>? = null) = flow<Pair<K, V?>> {

    val queryChannel = cquery.setChannelLocalListener(Channel<Pair<K, V?>>(channelCapacity, onBufferOverflow)) { event ->
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
        val items = if (sortKeysBy == null) { queryCursor } else { queryCursor.sortedWith(compareBy(sortKeysBy, { it.key })) }
        for (entry in items) {
            emit(Pair(entry.key, entry.value))
        }

        signalStartOfStream()

        emitAll(queryChannel)
    } finally {
        queryCursor.close()
        queryChannel.close()
    }
}

internal fun <K, V> IgniteCache<K, V>.iterateQuery(cquery: ScanQuery<K, V>, sortKeysBy: Comparator<in K>? = null) = flow<Pair<K, V>> {
    val queryCursor = query(cquery)
    try {
        val items = if (sortKeysBy == null) { queryCursor } else { queryCursor.sortedWith(compareBy(sortKeysBy, { it.key })) }
        for (entry in items) {
            emit(Pair(entry.key, entry.value))
        }
    } finally {
        queryCursor.close()
    }
}

internal suspend fun <K, V> IgniteCache<K, V>.optimisticUpdateSuspend(key: K, block: suspend (V?) -> V?) {
    while (true) {
        val value = this.getAsync(key).await()
        val newValue = block(value)
        if (this.replaceOrRemoveSuspend(key, value, newValue)) {
            break
        }
    }
}

internal suspend fun <K, V> IgniteCache<K, V>.replaceOrRemoveSuspend(key: K, oldValue: V?, newValue: V?) =
    if (oldValue == null) {
        if (newValue == null) {
            !(this.containsKeyAsync(key).await()!!) // removeIfAbsent
        } else {
            this.putIfAbsentAsync(key, newValue).await()!!
        }
    } else {
        if (newValue == null) {
            this.removeAsync(key, oldValue).await()!!
        } else {
            this.replaceAsync(key, oldValue, newValue).await()!!
        }
    }

internal suspend fun <K, V> IgniteCache<K, V>.putOrRemoveSuspend(key: K, newValue: V?): Boolean {
    if (newValue == null) {
        return this.removeAsync(key).await()!!
    } else {
        this.putAsync(key, newValue).await()
        return true
    }
}

internal suspend fun <K, V> IgniteCache<K, V>.getAndPutOrRemoveSuspend(key: K, newValue: V?) =
    if (newValue == null) {
        this.getAndRemoveAsync(key).await()
    } else {
        this.getAndPutAsync(key, newValue).await()
    }

internal suspend fun <K, V> IgniteCache<K, V>.getWhenPredicateSuspend(key: K, attemptEarlyExit: Boolean = true, localToData: Boolean = false, predicate: (V?) -> Boolean): V? {
    if (attemptEarlyExit) {
        val initialValue = if (!localToData) { getAsync(key).await() } else { localPeek(key, CachePeekMode.PRIMARY) }
        if (predicate(initialValue)) {
            return initialValue
        }
    }

    val cquery = ContinuousQuery<K, V>()

    cquery.remoteFilterFactory = Factory {
        CacheEntryEventFilter { event ->
            event.key == key && predicate(event.currentValue)
        }
    }
    cquery.setLocal(localToData)

    val queryChannel = cquery.setChannelLocalListener(Channel<V?>(Channel.CONFLATED)) { event -> send(event.currentValue) }
    val queryCursor = query(cquery)

    try {
        val currentValue = if (!localToData) { getAsync(key).await() } else { localPeek(key, CachePeekMode.PRIMARY) }
        if (predicate(currentValue)) {
            return currentValue
        } else {
            val matchedValue = queryChannel.receive()
            // assert(predicate(matchedValue))
            return matchedValue
        }
    } finally {
        queryCursor.close()
        queryChannel.close()
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
