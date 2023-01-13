package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.ExecutionData
import com.obecto.perper.model.IgniteAny
import com.obecto.perper.model.PerperError
import com.obecto.perper.model.PerperExecution
import com.obecto.perper.model.PerperExecutionData
import com.obecto.perper.model.PerperExecutionFilter
import com.obecto.perper.model.PerperExecutions
import com.obecto.perper.model.PerperInstance
import com.obecto.perper.model.toPerperErrorOrNull
import io.opentelemetry.api.OpenTelemetry
import io.opentelemetry.api.common.Attributes
import io.opentelemetry.api.trace.propagation.W3CTraceContextPropagator
import io.opentelemetry.api.trace.Span
import io.opentelemetry.context.propagation.ContextPropagators
import io.opentelemetry.exporter.otlp.metrics.OtlpGrpcMetricExporter
import io.opentelemetry.exporter.otlp.trace.OtlpGrpcSpanExporter
import io.opentelemetry.sdk.OpenTelemetrySdk
import io.opentelemetry.sdk.metrics.SdkMeterProvider
import io.opentelemetry.sdk.metrics.export.PeriodicMetricReader
import io.opentelemetry.sdk.resources.Resource
import io.opentelemetry.sdk.trace.SdkTracerProvider
import io.opentelemetry.sdk.trace.export.BatchSpanProcessor
import io.opentelemetry.semconv.resource.attributes.ResourceAttributes
import kotlinx.coroutines.Job
import kotlinx.coroutines.awaitCancellation
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

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
class PerperExecutionsIgniteImpl(val ignite: Ignite) : PerperExecutions {
    val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions").withKeepBinary<String, BinaryObject>()
    val binary = ignite.binary()
    val log = ignite.log().getLogger(this)

    val resource = Resource.getDefault()
        .merge(Resource.create(Attributes.of(ResourceAttributes.SERVICE_NAME, "perper-executions")))

    val sdkTracerProvider = SdkTracerProvider.builder()
        .addSpanProcessor(BatchSpanProcessor.builder(OtlpGrpcSpanExporter.builder().build()).build())
        .setResource(resource)
        .build()

    val sdkMeterProvider = SdkMeterProvider.builder()
        .registerMetricReader(PeriodicMetricReader.builder(OtlpGrpcMetricExporter.builder().build()).build())
        .setResource(resource)
        .build()

    val openTelemetry = OpenTelemetrySdk.builder()
        .setTracerProvider(sdkTracerProvider)
        .setMeterProvider(sdkMeterProvider)
        .setPropagators(ContextPropagators.create(W3CTraceContextPropagator.getInstance()))
        .buildAndRegisterGlobal()
    
    val tracer = openTelemetry.getTracer("io.opentelemetry.contrib.perper");
    
    override suspend fun create(instance: PerperInstance, delegate: String, arguments: List<IgniteAny?>, execution: PerperExecution?): PerperExecution {
        val executionNonNull = execution ?: PerperExecution("$delegate-${UUID.randomUUID()}")
        executionsCache.putAsync(
            executionNonNull.execution,
            binary.toBinary(
                ExecutionData(
                    agent = instance.agent,
                    instance = instance.instance,
                    delegate = delegate,
                    finished = false,
                    parameters = arguments.map({ it?.wrappedValue }).toTypedArray()
                )
            )
        ).await()
        return executionNonNull
    }

    override suspend fun getResult(execution: PerperExecution): Pair<List<IgniteAny?>, PerperError?>? {
        val executionData = executionsCache.getWhenPredicateSuspend(execution.execution) {
            it == null || it.field<Boolean>("finished")
        }?.deserialize<ExecutionData>()

        if (executionData == null) {
            return null
        } else {
            return Pair(executionData.results?.map(::IgniteAny) ?: emptyList(), executionData.error.toPerperErrorOrNull())
        }
    }

    override suspend fun complete(execution: PerperExecution, results: List<IgniteAny?>, error: PerperError?) {
        executionsCache.optimisticUpdateSuspend(execution.execution) { value ->
            if (value == null) {
                null
            } else {
                val executionData = value.deserialize<ExecutionData>()
                executionData.results = results.map({ it?.wrappedValue }).toTypedArray()
                executionData.error = error?.message
                executionData.finished = true
                binary.toBinary(executionData)
            }
        }
    }

    override suspend fun delete(execution: PerperExecution) {
        executionsCache.removeAsync(execution.execution).await()
    }

    override fun listen(filter: PerperExecutionFilter) = channelFlow<PerperExecutionData?>
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

        var executionsFlow =
            executionsCache.iterateQuery(query, signalStartOfStream = { send(null) }).map({ pair ->
                Pair(pair.first, pair.second?.deserialize<ExecutionData>())
            })

        if (filter.reserveAsWorkgroup != null) {
            executionsFlow = reserveExecutions(executionsFlow, filter.reserveAsWorkgroup)
        }

        val sentExecutions = HashMap<String, PerperExecutionData>()
        val executionsSpan = HashMap<String, Span>()
        executionsFlow.collect { pair ->
            val (key, value) = pair

            if (value != null) {
                if (!sentExecutions.containsKey(key)) {
                    val execution = PerperExecutionData(
                        instance = PerperInstance(value.agent, value.instance),
                        delegate = value.delegate,
                        execution = PerperExecution(key),
                        arguments = value.parameters?.map(::IgniteAny) ?: emptyList()
                    )
                    sentExecutions.put(key, execution)
                    send(execution)

                    val span = tracer.spanBuilder(value.delegate).startSpan()
                    executionsSpan.put(key, span)
                }
            } else {
                val execution = sentExecutions.remove(key)
                execution?.cancel()

                val span = executionsSpan.remove(key)
                span?.end()
            }
        }
    }

    override suspend fun reserve(execution: PerperExecution, workgroup: String, block: suspend () -> Unit) {
        ignite.withLockSuspend("executions-${execution.execution}-$workgroup") {
            block()
        }
    }

    private fun reserveExecutions(unreservedExecutionsFlow: Flow<Pair<String, ExecutionData?>>, asWorkgroup: String): Flow<Pair<String, ExecutionData?>> {
        val semaphore = Semaphore(1)
        val reserveJobs = HashMap<String, Job>()

        // We use a channelFlow so that we can emit executions from within withLockSuspend,
        // followed by a normal flow so that we can wait for executions to be acknowledged before we release the semaphore
        val reservedExecutionsFlow = channelFlow<Pair<String, ExecutionData?>> {
            unreservedExecutionsFlow.collect { pair ->
                if (pair.second == null) {
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
                if (pair.second != null) {
                    semaphore.release()
                }
            }
        }
    }
}
