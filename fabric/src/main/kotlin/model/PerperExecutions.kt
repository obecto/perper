package com.obecto.perper.model
import kotlinx.coroutines.Job
import kotlinx.coroutines.flow.Flow

data class PerperExecutionFilter(
    val agent: String,
    val instance: String? = null,
    val delegate: String? = null,
    val localToData: Boolean = false,
    val reserveAsWorkgroup: String? = null
)

class PerperExecutionData(
    val instance: PerperInstance,
    val delegate: String,
    val execution: PerperExecution,
    val arguments: Array<Any>
) {
    private val job = Job()

    val cancelled: Boolean
        get() = job.isCancelled

    fun cancel() {
        job.cancel()
    }

    fun invokeOnCancellation(block: () -> Unit) = job.invokeOnCompletion({ block() })
}

interface PerperExecutions {
    suspend fun create(instance: PerperInstance, delegate: String, arguments: Array<Any>, execution: PerperExecution? = null): PerperExecution

    suspend fun getResult(execution: PerperExecution): Pair<Array<Any>, PerperError?>?

    suspend fun complete(execution: PerperExecution, results: Array<Any>, error: PerperError?)

    suspend fun delete(execution: PerperExecution)

    suspend fun reserve(execution: PerperExecution, workgroup: String, block: suspend () -> Unit)

    fun listen(filter: PerperExecutionFilter): Flow<PerperExecutionData?> // Null signals start-of-stream
}
