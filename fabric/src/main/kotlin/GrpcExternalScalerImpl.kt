package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.ExecutionData
import kotlinx.coroutines.awaitCancellation
import kotlinx.coroutines.flow.channelFlow
import kotlinx.coroutines.flow.debounce
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.lang.IgniteBiPredicate
import sh.keda.externalscaler.ExternalScalerGrpcKt
import sh.keda.externalscaler.GetMetricSpecResponse
import sh.keda.externalscaler.GetMetricsRequest
import sh.keda.externalscaler.GetMetricsResponse
import sh.keda.externalscaler.IsActiveResponse
import sh.keda.externalscaler.ScaledObjectRef
import java.util.concurrent.atomic.AtomicInteger
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter
import javax.cache.event.CacheEntryUpdatedListener

class GrpcExternalScalerImpl(val ignite: Ignite) : ExternalScalerGrpcKt.ExternalScalerCoroutineImplBase() {

    val log = ignite.log()
    val ScaledObjectRef.agent
        get() = scalerMetadataMap.getOrDefault("agent", name)
    val ScaledObjectRef.isLocal
        get() = scalerMetadataMap.getOrDefault("local", "false") == "true"

    override suspend fun isActive(request: ScaledObjectRef): IsActiveResponse {
        val agent = request.agent
        val isLocal = request.isLocal

        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")
        val query = ScanQuery(IgniteBiPredicate<String, ExecutionData> { _, value -> value.agent == agent || (value.agent == "Registry" && value.instance == agent) }).also {
            it.setLocal(isLocal)
        }
        val queryCursor = executionsCache.query(query)
        try {
            val hasAvailable = queryCursor.any()
            return IsActiveResponse.newBuilder().also {
                it.result = hasAvailable
            }.build()
        } finally {
            queryCursor.close()
        }
    }

    @OptIn(kotlinx.coroutines.FlowPreview::class, kotlinx.coroutines.ExperimentalCoroutinesApi::class)
    override fun streamIsActive(request: ScaledObjectRef) = channelFlow<Boolean> {
        // NOTE: QUESTION: Should we report IsActive as true for a ScaledObjectRef that only includes targetExecutions but has active instances?
        // .. Currently, we do, but maybe that's not optimal for KEDA?

        val agent = request.agent
        val isLocal = request.isLocal

        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

        var count = AtomicInteger(0)

        val query = ContinuousQuery<String, ExecutionData>()
        query.remoteFilterFactory = Factory { CacheEntryEventFilter { event -> event.value.agent == agent || (event.value.agent == "Registry" && event.value.instance == agent) } }
        query.localListener = CacheEntryUpdatedListener { events ->
            for (event in events) {
                if (event.eventType.isRemoval && count.decrementAndGet() == 0) {
                    runBlocking { send(false) }
                } else if (event.eventType.isCreation && count.incrementAndGet() == 1) {
                    runBlocking { send(true) }
                }
            }
        }
        query.setLocal(isLocal)
        query.initialQuery = ScanQuery(IgniteBiPredicate<String, ExecutionData> { _, value -> value.agent == agent || (value.agent == "Registry" && value.instance == agent) }).also {
            it.setLocal(isLocal)
        }
        val queryCursor = executionsCache.query(query)
        try {
            send(false)
            for (entry in queryCursor) {
                if (count.incrementAndGet() == 1) {
                    send(true)
                }
            }
            awaitCancellation()
        } finally {
            queryCursor.close()
        }
    }.debounce(100).map({ value ->
        IsActiveResponse.newBuilder().also {
            it.result = value
        }.build()
    })

    override suspend fun getMetricSpec(request: ScaledObjectRef): GetMetricSpecResponse {
        val agent = request.agent
        return GetMetricSpecResponse.newBuilder().also {
            fun addMetric(metricName: String, scalerMetadataProperty: String) {
                val targetValue = request.scalerMetadataMap.get(scalerMetadataProperty)?.toLong()
                if (targetValue != null) {
                    it.addMetricSpecsBuilder().also {
                        it.metricName = metricName
                        it.targetSize = targetValue
                    }
                }
            }

            addMetric("$agent-instances", "targetInstances")
            addMetric("$agent-executions", "targetExecutions")
        }.build()
    }

    override suspend fun getMetrics(request: GetMetricsRequest): GetMetricsResponse {
        // NOTE: We aren't using request.metricName yet -- but it might be useful for performance optimizations?
        val agent = request.scaledObjectRef.agent
        val isLocal = request.scaledObjectRef.isLocal

        val executionsCache = ignite.getOrCreateCache<String, ExecutionData>("executions")

        val query = ScanQuery(IgniteBiPredicate<String, ExecutionData> { _, value -> value.agent == agent || (value.agent == "Registry" && value.instance == agent) }).also {
            it.setLocal(isLocal)
        }
        val queryCursor = executionsCache.query(query)

        try {
            var instances = 0
            var executions = 0

            for (entry in queryCursor) {
                if (entry.value.agent == "Registry" && entry.value.instance == agent) {
                    instances ++
                }
                if (entry.value.agent == agent) {
                    executions ++
                }
            }

            return GetMetricsResponse.newBuilder().also {
                it.addMetricValuesBuilder().also {
                    it.metricName = "$agent-instances"
                    it.metricValue = instances.toLong()
                }
                it.addMetricValuesBuilder().also {
                    it.metricName = "$agent-executions"
                    it.metricValue = executions.toLong()
                }
            }.build()
        } finally {
            queryCursor.close()
        }
    }
}
