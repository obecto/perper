package com.obecto.perper.fabric
import io.grpc.kotlin.GrpcContextElement
import kotlinx.coroutines.CompletableJob
import kotlinx.coroutines.Job
import java.util.concurrent.atomic.AtomicLong
import java.util.concurrent.atomic.AtomicReference
import kotlin.coroutines.CoroutineContext

fun CoroutineContext.requestId() = System.identityHashCode(this[GrpcContextElement]).toString(16)

class SemaphoreWithArbitraryRelease(permits: Long) { // NOTE: Likely not a fair Semaphore
    private var _permits = AtomicLong(permits)
    private var _wait = AtomicReference<CompletableJob?>()

    public suspend fun acquire(count: Long = 1) {
        while (true) {
            val newValue = _permits.addAndGet(-count)
            if (newValue < 0) {
                _permits.addAndGet(count) // Revert
                (_wait.updateAndGet { oldValue -> oldValue ?: Job() })!!.join()
                continue
            }
            break
        }
    }

    public fun release(count: Long = 1) {
        _permits.addAndGet(count)
        _wait.getAndSet(null)?.complete()
    }
}

object Ticks {
    val startTicks = (System.currentTimeMillis()) * 10_000
    val startNanos = System.nanoTime()
    fun getCurrentTicks() = startTicks + (System.nanoTime() - startNanos) / 100
}
