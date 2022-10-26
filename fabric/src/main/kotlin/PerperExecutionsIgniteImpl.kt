package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.ExecutionData
import com.obecto.perper.model.PerperError
import com.obecto.perper.model.PerperExecution
import com.obecto.perper.model.PerperExecutionData
import com.obecto.perper.model.PerperExecutionFilter
import com.obecto.perper.model.PerperExecutions
import com.obecto.perper.model.PerperInstance
import com.obecto.perper.model.toPerperErrorOrNull
import kotlinx.coroutines.Job
import kotlinx.coroutines.awaitCancellation
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.channelFlow
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.launch
import kotlinx.coroutines.sync.Semaphore
import org.apache.ignite.Ignite
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.query.ContinuousQuery
import java.util.UUID
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
class PerperExecutionsIgniteImpl(val ignite: Ignite) : PerperExecutions {
    val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions").withKeepBinary<String, BinaryObject>()
    val binary = ignite.binary()
    val log = ignite.log().getLogger(this)

    override suspend fun create(instance: PerperInstance, delegate: String, arguments: Array<Any>, execution: PerperExecution?): PerperExecution {
        val executionNonNull = execution ?: PerperExecution("$delegate-${UUID.randomUUID()}")
        executionsCache.putAsync(
            executionNonNull.execution,
            binary.toBinary(
                ExecutionData(
                    agent = instance.agent,
                    instance = instance.instance,
                    delegate = delegate,
                    finished = false,
                    parameters = arguments
                )
            )
        ).await()
        return executionNonNull
    }

    override suspend fun getResult(execution: PerperExecution): Pair<Array<Any>, PerperError?>? {
        val query = ContinuousQuery<String, BinaryObject>()

        val key = execution.execution

        query.remoteFilterFactory = Factory {
            CacheEntryEventFilter { event ->
                event.key == key && (event.eventType.isRemoval || event.value.field<Boolean>("finished"))
            }
        }

        val queryChannel = query.setChannelLocalListener(Channel<Unit>(Channel.CONFLATED)) { _ -> send(Unit) }
        val queryCursor = executionsCache.query(query) // We start a query first to avoid a race where the execution is completed between us checking and submitting the query

        try {
            var executionData = executionsCache.getAsync(execution.execution).await()?.deserialize<ExecutionData>()

            if (!(executionData == null || executionData.finished)) {
                queryChannel.receive()
                executionData = executionsCache.getAsync(execution.execution).await()?.deserialize<ExecutionData>()
            }

            if (executionData == null) {
                return null
            } else {
                return Pair(executionData.results ?: emptyArray(), executionData.error.toPerperErrorOrNull())
            }
        } finally {
            queryCursor.close()
            queryChannel.close()
        }
    }

    override suspend fun complete(execution: PerperExecution, results: Array<Any>, error: PerperError?) {
        executionsCache.optimisticUpdateSuspend(execution.execution) { value ->
            if (value == null) {
                null
            } else {
                val executionData = value.deserialize<ExecutionData>()
                executionData.results = results
                executionData.error = error?.message
                executionData.finished = true
                binary.toBinary(executionData)
            }
        }
    }

    override suspend fun delete(execution: PerperExecution) {
        executionsCache.removeAsync(execution.execution).await()
    }

    override suspend fun listen(filter: PerperExecutionFilter) = flow<PerperExecutionData?>
    {
        val query = ContinuousQuery<String, BinaryObject>()

        val agent = filter.agent
        val instance = filter.instance
        val delegate = filter.delegate

        query.setScanAndRemoteFilter { _, currentValue, oldValue ->
            if (currentValue != null) {
                if (oldValue != null && !oldValue.field<Boolean>("finished")) {
                    false
                } else {
                    !currentValue.field<Boolean>("finished") &&
                        currentValue.field<String>("agent") == agent &&
                        (instance == null || currentValue.field<String>("instance") == instance) &&
                        (delegate == null || currentValue.field<String>("delegate") == delegate)
                }
            } else {
                true
            }
        }

        var executionsFlow: Flow<Pair<String, ExecutionData?>?> =
            executionsCache.iterateQuery(query).map {
                if (it == null) { it } else { Pair(it.first, it.second?.deserialize<ExecutionData>()) }
            }

        if (filter.reserveAsWorkgroup != null) {
            executionsFlow = reserveExecutions(executionsFlow, filter.reserveAsWorkgroup)
        }

        val sentExecutions = HashMap<String, PerperExecutionData>()
        executionsFlow.collect { pair ->
            if (pair == null) {
                emit(null)
            } else {
                val (key, value) = pair

                if (value != null) {
                    if (!sentExecutions.containsKey(key)) {
                        val execution = PerperExecutionData(
                            instance = PerperInstance(value.agent, value.instance),
                            delegate = value.delegate,
                            execution = PerperExecution(key),
                            arguments = value.parameters ?: emptyArray()
                        )
                        sentExecutions.put(key, execution)
                        emit(execution)
                    }
                } else {
                    val execution = sentExecutions.remove(key)
                    execution?.cancel()
                }
            }
        }
    }

    override suspend fun reserve(execution: PerperExecution, workgroup: String, block: suspend () -> Unit) {
        ignite.withLockSuspend("executions-${execution.execution}-$workgroup") {
            block()
        }
    }

    private fun reserveExecutions(unreservedExecutionsFlow: Flow<Pair<String, ExecutionData?>?>, asWorkgroup: String): Flow<Pair<String, ExecutionData?>?> {
        val semaphore = Semaphore(1)
        val reserveJobs = HashMap<String, Job>()

        // We use a channelFlow so that we can emit executions from within withLockSuspend,
        // followed by a normal flow so that we can wait for executions to be acknowledged before we release the semaphore
        val reservedExecutionsFlow = channelFlow<Pair<String, ExecutionData?>?> {
            unreservedExecutionsFlow.collect { pair ->
                if (pair == null) {
                    send(pair)
                } else if (pair.second == null) {
                    reserveJobs.get(pair.first)?.cancel()
                    send(pair)
                } else {
                    reserveJobs.getOrPut(pair.first) {
                        launch {
                            semaphore.acquire() // We acquire the semaphore before we attempt locking, to prevent starting unneccesary lock attempts
                            var semaphoreHeld = true
                            try {
                                ignite.withLockSuspend("executions-${pair.first}-$asWorkgroup", fastLockFailBlock = {
                                    semaphore.release()
                                    semaphoreHeld = false
                                }) {
                                    if (!semaphoreHeld) {
                                        semaphore.acquire()
                                        semaphoreHeld = true
                                    }
                                    send(pair)
                                    semaphoreHeld = false // Once we send the execution, the semaphore passes to the emit-release loop below
                                    awaitCancellation()
                                }
                            } finally {
                                if (semaphoreHeld) {
                                    semaphore.release()
                                    semaphoreHeld = false
                                }
                            }
                        }
                    }
                }
            }
        }
        return flow {
            reservedExecutionsFlow.collect { pair ->
                emit(pair)
                if (pair != null && pair.second != null) {
                    semaphore.release()
                }
            }
        }
    }
}
