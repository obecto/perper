package com.obecto.perper.fabric
import com.google.protobuf.Empty
import com.obecto.perper.fabric.cache.ExecutionData
import com.obecto.perper.fabric.cache.StreamListener
import com.obecto.perper.protobuf.AllExecutionsResponse
import com.obecto.perper.protobuf.ExecutionFinishedRequest
import com.obecto.perper.protobuf.ExecutionsRequest
import com.obecto.perper.protobuf.ExecutionsResponse
import com.obecto.perper.protobuf.FabricGrpcKt
import com.obecto.perper.protobuf.ListenerAttachedRequest
import com.obecto.perper.protobuf.ReserveExecutionRequest
import com.obecto.perper.protobuf.ReservedExecutionsRequest
import com.obecto.perper.protobuf.StreamItemsRequest
import com.obecto.perper.protobuf.StreamItemsResponse
import kotlinx.coroutines.CompletableDeferred
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.NonCancellable
import kotlinx.coroutines.awaitCancellation
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.currentCoroutineContext
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.FlowCollector
import kotlinx.coroutines.flow.channelFlow
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.onEach
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import kotlinx.coroutines.withContext
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteInterruptedException
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.lang.IgniteBiPredicate
import java.lang.InterruptedException
import java.lang.Thread
import java.util.concurrent.CancellationException
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter
import kotlin.concurrent.thread

class Grpc1FabricImpl(val ignite: Ignite) : FabricGrpcKt.FabricCoroutineImplBase() {

    val log = ignite.log().getLogger(this)

    private fun Flow<ExecutionsResponse>.filterRemovedExecutions(): Flow<ExecutionsResponse> {
        val sentExecutions = HashSet<String>()
        return filter { execution ->
            if (execution.cancelled) {
                sentExecutions.remove(execution.execution)
            } else {
                sentExecutions.add(execution.execution)
            }
        }
    }

    private fun Flow<ExecutionsResponse>.logExecutionParameters(): Flow<ExecutionsResponse> {
        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions").withKeepBinary<String, BinaryObject>()
        if (!log.isDebugEnabled()) {
            return this
        } else {
            return onEach { execution ->
                if (execution.execution == "") {
                    // Pass
                } else if (execution.cancelled) {
                    log.debug("Sending execution; instance=${execution.instance} delegate=${execution.delegate} execution=${execution.execution} cancelled=true request=${currentCoroutineContext().requestId()}")
                } else {
                    val executionDataBinary = executionsCache.get(execution.execution)
                    log.debug("Sending execution; instance=${execution.instance} delegate=${execution.delegate} execution=${execution.execution} parameters=${executionDataBinary?.field<Array<Any>>("parameters")?.joinToString(", ", "[", "]")} request=${currentCoroutineContext().requestId()}")
                }
            }
        }
    }

    private fun _executionsFilter(request: ExecutionsRequest): (ExecutionData) -> Boolean {
        val agent = request.agent
        val instance = if (request.instance != "") request.instance else null
        val delegate = if (request.delegate != "") request.delegate else null
        return { value ->
            !value.finished &&
                value.agent == agent &&
                (instance == null || value.instance == instance) &&
                (delegate == null || value.delegate == delegate)
        }
    }

    private fun _executions(request: ExecutionsRequest) = flow<ExecutionsResponse> {
        val agent = request.agent
        val instance = if (request.instance != "") request.instance else null
        val filterValue = _executionsFilter(request)
        val localToData = request.localToData

        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

        val query = ContinuousQuery<String, ExecutionData>()
        val queryChannel = query.setChannelLocalListener(Channel<Pair<String, Pair<Boolean, ExecutionData>>>(Channel.UNLIMITED)) {
            event ->
            send(Pair(event.key, Pair(event.eventType.isRemoval, event.value)))
        }
        query.setLocal(localToData)

        query.remoteFilterFactory = Factory {
            CacheEntryEventFilter { event ->
                if (event.eventType.isRemoval)
                    true
                else if (event.isOldValueAvailable && !event.oldValue.finished)
                    false
                else // NOTE: Can probably use a single query for all executions
                    filterValue(event.value)
            }
        }
        query.initialQuery = ScanQuery<String, ExecutionData>().also {
            it.setLocal(localToData)
            it.filter = IgniteBiPredicate<String, ExecutionData> { _, value ->
                filterValue(value)
            }
        }

        log.debug({ "Listening executions; agent=$agent instance=$instance delegate=${request.delegate} request=${currentCoroutineContext().requestId()}" })

        val queryCursor = executionsCache.query(query)

        try {
            suspend fun FlowCollector<ExecutionsResponse>.output(key: String, value: ExecutionData, removed: Boolean) {
                emit(
                    ExecutionsResponse.newBuilder().also {
                        it.instance = value.instance
                        it.delegate = value.delegate
                        it.execution = key
                        it.cancelled = removed
                    }.build()
                )
            }

            for (entry in queryCursor) {
                output(entry.key, entry.value, false)
            }

            emit(ExecutionsResponse.newBuilder().also { it.startOfStream = true }.build())

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
            log.debug({ "Listening executions stopped; agent=$agent instance=$instance delegate=${request.delegate} request=${currentCoroutineContext().requestId()}" })
            queryCursor.close()
            queryChannel.close()
        }
    }

    override fun executions(request: ExecutionsRequest) = _executions(request)
        .filterRemovedExecutions()
        .logExecutionParameters()

    override suspend fun allExecutions(request: ExecutionsRequest): AllExecutionsResponse {
        val filterValue = _executionsFilter(request)
        val localToData = request.localToData

        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

        val query = ScanQuery<String, ExecutionData>().also {
            it.setLocal(localToData)
            it.filter = IgniteBiPredicate<String, ExecutionData> { _, value ->
                filterValue(value)
            }
        }

        log.debug({ "All executions requested; agent=${request.agent} instance=${request.instance} delegate=${request.delegate} request=${currentCoroutineContext().requestId()}" })

        val queryCursor = executionsCache.query(query)

        try {
            return AllExecutionsResponse.newBuilder().also {
                it.addAllExecutions(
                    queryCursor.map({ entry ->
                        ExecutionsResponse.newBuilder().also {
                            it.instance = entry.value.instance
                            it.delegate = entry.value.delegate
                            it.execution = entry.key
                        }.build()
                    })
                )
            }.build()
        } catch (e: CancellationException) {
            throw e
        } catch (e: Exception) {
            log.error(e.toString())
            e.printStackTrace()
            throw e
        } finally {
            queryCursor.close()
        }
    }

    override suspend fun executionFinished(request: ExecutionFinishedRequest): Empty {
        val execution = request.execution

        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

        val query = ContinuousQuery<String, ExecutionData>()
        val queryChannel = query.setChannelLocalListener(Channel<Unit>(Channel.CONFLATED)) { _ -> send(Unit) }

        query.remoteFilterFactory = Factory {
            CacheEntryEventFilter {
                event ->
                event.key == execution && (event.eventType.isRemoval || event.value.finished)
            }
        }

        val queryCursor = executionsCache.query(query) // Important to start query before checking if it is already finished

        log.debug({ "Listening execution finishing; execution=$execution request=${currentCoroutineContext().requestId()}" })

        try {
            val executionData = executionsCache.get(execution)
            if (!(executionData == null || executionData.finished)) {
                queryChannel.receive()
            }

            if (log.isDebugEnabled()) {
                val executionDataBinary = executionsCache.withKeepBinary<String, BinaryObject>().get(execution)
                log.debug("Execution finished; instance=${executionData?.instance} delegate=${executionData?.delegate} execution=$execution result=${executionDataBinary?.field<Array<Any>>("result")?.joinToString(", ", "[", "]")} request=${currentCoroutineContext().requestId()}")
            }

            return Empty.getDefaultInstance()
        } finally {
            log.debug({ "Listening execution finishing stopped; request=${currentCoroutineContext().requestId()}" })
            queryCursor.close()
            queryChannel.close()
        }
    }

    suspend fun reserveExecution(execution: String, workGroup: String, fastLockFailBlock: suspend () -> Unit = {}, block: suspend () -> Unit) {
        val locked = Channel<Boolean>(Channel.CONFLATED)

        var finished = false
        val lock = Object()

        lateinit var lockThread: Thread
        lockThread = thread { // Sigh. Ignite locks are thread-bound and don't care about async usage
            try {
                val executionLock = ignite.reentrantLock("executions-$workGroup-$execution", true, false, true)
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
                    Thread.interrupted() // Clear interrupted status
                    executionLock.unlock()
                }
            } catch (_: IgniteInterruptedException) {
            } catch (_: InterruptedException) {
            }
        }

        try {
            while (true) {
                val lockedSuccessfully = locked.receive()

                if (lockedSuccessfully) {
                    block()
                    break
                } else {
                    fastLockFailBlock()
                }
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

    override fun reserveExecution(request: ReserveExecutionRequest) = flow<Empty> {
        val execution = request.execution
        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

        reserveExecution(execution, request.workGroup) {
            val executionData = executionsCache.get(execution)
            if (executionData == null || executionData.finished) {
                throw CancellationException()
            }
            emit(Empty.getDefaultInstance())
            awaitCancellation()
        }
    }

    @OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
    override fun reservedExecutions(requests: Flow<ReservedExecutionsRequest>) = channelFlow<ExecutionsResponse> {
        coroutineScope {
            val executions = CompletableDeferred<Flow<ExecutionsResponse>>()
            var workGroup = ""
            var semaphore = SemaphoreWithArbitraryRelease(0)

            launch {
                requests.collect { request ->
                    if (request.workGroup != "") {
                        workGroup = request.workGroup
                    }
                    if (request.reserveNext != 0L) {
                        semaphore.release(request.reserveNext)
                    }
                    if (request.hasFilter()) {
                        executions.complete(_executions(request.filter))
                    }
                }
            }

            val reserveExecutionJobs = HashMap<String, Job>()

            executions.await().collect { execution ->
                if (execution.startOfStream) {
                    send(execution)
                } else if (execution.cancelled) {
                    val reserveJob = reserveExecutionJobs.remove(execution.execution)
                    if (reserveJob != null) {
                        reserveJob.cancel()
                        send(execution)
                    }
                } else {
                    reserveExecutionJobs.getOrPut(execution.execution) {
                        launch {
                            semaphore.acquire(1)
                            var semaphoreHeld = true
                            try {
                                reserveExecution(execution.execution, workGroup, fastLockFailBlock = {
                                    semaphore.release(1)
                                    semaphoreHeld = false
                                }) {
                                    if (!semaphoreHeld) {
                                        semaphore.acquire(1)
                                        semaphoreHeld = true
                                    }
                                    send(execution)
                                    semaphoreHeld = false // Once we send the execution, the semaphore token is "held" by the client; and will be released with the next reserveNext message
                                    awaitCancellation()
                                }
                            } finally {
                                if (semaphoreHeld) {
                                    semaphore.release(1)
                                    semaphoreHeld = false
                                }
                            }
                        }
                    }
                }
            }
        }
    }
        .filterRemovedExecutions()
        .logExecutionParameters()

    @OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
    override fun streamItems(request: StreamItemsRequest) = flow<StreamItemsResponse> {
        val stream = request.stream
        val startKey = if (request.startKey != -1L) request.startKey else null
        val stride = request.stride
        val localToData = request.localToData

        suspend fun FlowCollector<StreamItemsResponse>.output(key: Long) {
            emit(
                StreamItemsResponse.newBuilder().also {
                    it.key = key
                }.build()
            )
        }

        val streamCache = ignite.cache<Long, Any>(stream).withKeepBinary<Long, Any>()

        if (stride == 0L) {
            val query = ContinuousQuery<Long, Any>()
            val queryChannel = query.setChannelLocalListener(Channel<Long>(Channel.UNLIMITED)) { event -> send(event.key) }
            query.setLocal(localToData)

            if (startKey == null) {
                query.remoteFilterFactory = Factory {
                    CacheEntryEventFilter {
                        event ->
                        event.eventType.isCreation
                    }
                }
                // query.initialQuery = null;
            } else {
                query.remoteFilterFactory = Factory {
                    CacheEntryEventFilter {
                        event ->
                        event.eventType.isCreation && event.key >= startKey
                    }
                }
                query.initialQuery = ScanQuery(IgniteBiPredicate<Long, Any> { key, _ -> key > startKey }).also {
                    it.setLocal(localToData)
                }
            }

            val queryCursor = streamCache.query(query)
            log.debug({ "Listening stream items; stream=$stream start=${startKey ?: "(last)"} stride=(any) request=${currentCoroutineContext().requestId()}" })

            try {
                val itemKeys = queryCursor.map({ item -> item.key }).toMutableList() // NOTE: Sorts all keys in-memory; inefficient
                itemKeys.sort()

                for (key in itemKeys) {
                    output(key)
                }
                var lastKey = itemKeys.lastOrNull() ?: startKey?.minus(1) // -1 so that we still deliver the item at the start key

                for (key in queryChannel) {
                    if (lastKey == null || key > lastKey) {
                        lastKey = key
                        output(key)
                    }
                }
            } finally {
                log.debug({ "Listening stream items stopped; stream=$stream start=${startKey ?: "(last)"} stride=(any) request=${currentCoroutineContext().requestId()}" })
                queryCursor.close()
                queryChannel.close()
            }
        } else // if (stride != 0L)
            {
                log.debug({ "Listening stream items; stream=$stream start=${startKey ?: "(last)"} stride=$stride request=${currentCoroutineContext().requestId()}" })
                try {
                    var keyOrNull = startKey

                    if (keyOrNull == null) {
                        val keys = streamCache.query(
                            ScanQuery<Long, Any>().also {
                                it.setLocal(localToData)
                            }
                        ).map({ item -> item.key })
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
                        if (!streamCache.containsKey(key)) {
                            val query = ContinuousQuery<Long, Any>()
                            val queryChannel = query.setChannelLocalListener(Channel<Unit>(Channel.CONFLATED)) { _ -> send(Unit) }

                            query.remoteFilterFactory = Factory {
                                CacheEntryEventFilter {
                                    event ->
                                    event.key == key
                                }
                            }

                            val queryCursor = streamCache.query(query) // Important to start query before checking if it is already finished

                            try {
                                if (!streamCache.containsKey(key)) { // Race check
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
                    log.debug({ "Listening stream items stopped; stream=$stream start=${startKey ?: "(last)"} stride=$stride request=${currentCoroutineContext().requestId()}" })
                }
            }
    }

    override suspend fun listenerAttached(request: ListenerAttachedRequest): Empty {
        val stream = request.stream

        val streamListenersCacheName = "stream-listeners"
        val streamListenersCache = ignite.cache<String, StreamListener>(streamListenersCacheName)

        val query = ContinuousQuery<String, StreamListener>()
        val queryChannel = query.setChannelLocalListener(Channel<Unit>(Channel.CONFLATED)) { _ -> send(Unit) }

        query.remoteFilterFactory = Factory {
            CacheEntryEventFilter {
                event ->
                event.eventType.isCreation && event.value.stream == stream
            }
        }
        query.initialQuery = ScanQuery(IgniteBiPredicate<String, StreamListener> { _, value -> value.stream == stream })

        val queryCursor = streamListenersCache.query(query)

        log.debug({ "Listening attached listener; stream=$stream request=${currentCoroutineContext().requestId()}" })

        try {
            for (_i in queryCursor) {
                return Empty.getDefaultInstance()
            }
            queryChannel.receive()
            return Empty.getDefaultInstance()
        } finally {
            log.debug({ "Listening attached listener stopped; stream=$stream request=${currentCoroutineContext().requestId()}" })
            queryCursor.close()
            queryChannel.close()
        }
    }
}
