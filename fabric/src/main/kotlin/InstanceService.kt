package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.AgentType
import com.obecto.perper.fabric.cache.InstanceData
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.lang.IgniteBiPredicate
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import java.lang.ProcessBuilder.Redirect
import javax.cache.event.CacheEntryUpdatedListener
import javax.cache.event.EventType
import kotlin.collections.toList
import kotlin.streams.asSequence

class InstanceService(var composeFile: String = "docker-compose.yml") : JobService() {

    companion object Caches {
        fun setInstanceRunning(ignite: Ignite, instance: String, running: Boolean) {
            val runningInstancesCache = ignite.getOrCreateCache<String, Boolean>("runningInstances")
            if (running) {
                runningInstancesCache.put(instance, true)
            } else {
                runningInstancesCache.remove(instance)
            }
        }
        // NOTE: It is important to call updateAgentRunning before updateAgentType
        fun setAgentType(ignite: Ignite, agent: String, state: AgentType) {
            val agentTypesCache = ignite.getOrCreateCache<String, AgentType>("agentTypes")
            agentTypesCache.put(agent, state)
        }
    }

    lateinit var log: IgniteLogger

    lateinit var ignite: Ignite

    lateinit var instancesCache: IgniteCache<String, InstanceData>
    lateinit var agentTypesCache: IgniteCache<String, AgentType>
    lateinit var runningInstancesCache: IgniteCache<String, Boolean>

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

    override fun init(ctx: ServiceContext) {
        instancesCache = ignite.getOrCreateCache<String, InstanceData>("instances")
        agentTypesCache = ignite.getOrCreateCache("agentTypes")
        runningInstancesCache = ignite.getOrCreateCache("runningInstances")

        super.init(ctx)
    }

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        launch {
            val query = ContinuousQuery<String, InstanceData>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    coroutineScope.launch {
                        log.debug({ "Agent object modified '${event.key}' ${event.value != null}" }) // TODO: What if an instance's agent changes?
                        updateAgent(event.key, if (event.eventType != EventType.REMOVED) event.value else null)
                    }
                }
            }
            instancesCache.query(query)
            log.debug({ "Instances listener started!" })
        }
        launch {
            val query = ContinuousQuery<String, AgentType>()
            query.localListener = CacheEntryUpdatedListener { events ->
                for (event in events) {
                    if (event.oldValue != AgentType.CONTAINERS && event.value == AgentType.CONTAINERS) {
                        coroutineScope.launch {
                            val oldAgentsQuery = ScanQuery<String, InstanceData>()
                            oldAgentsQuery.filter = IgniteBiPredicate<String, InstanceData> { _, agent -> agent.agent == event.key }
                            for (agent in instancesCache.query(oldAgentsQuery)) {
                                updateAgent(agent.key, agent.value)
                            }
                        }
                    }
                }
            }
            agentTypesCache.query(query)
            log.debug({ "Agent types listener started!" })
        }
    }

    suspend fun updateAgent(instance: String, instanceData: InstanceData?) {
        if (instanceData == null) {
            stopInstance(instance)
        } else {
            val agent = instanceData.agent
            val tc = agentTypesCache.getAndPutIfAbsent(agent, AgentType.STARTING)
            when (tc) {
                null -> startInstance(agent, instance)
                AgentType.STARTING -> {}
                AgentType.FUNCTIONS -> {} // Notification?
                AgentType.CONTAINERS -> {
                    when (runningInstancesCache.getAndPutIfAbsent(instance, false)) {
                        null -> startInstance(agent, instance)
                        else -> {}
                    }
                }
            }
        }
    }

    suspend fun startInstance(agent: String, instance: String) {
        log.debug({ "Starting instance '$instance' of '$agent'" })
        val serviceName = agent
        val process = ProcessBuilder(
            "docker-compose", "-f", composeFile, "run",
            "--rm", "--detach",
            "-l", "com.obecto.perper.agent=" + agent,
            "-e", "X_PERPER_AGENT=" + agent,
            "-l", "com.obecto.perper.instance=" + instance,
            "-e", "X_PERPER_INSTANCE=" + instance,
            serviceName
        )
            .redirectOutput(Redirect.PIPE)
            .redirectError(if (log.isDebugEnabled()) Redirect.PIPE else Redirect.DISCARD)
            .start()

        if (log.isDebugEnabled()) {
            process.errorStream.bufferedReader().lines().forEachOrdered({ x -> log.debug("docker compose: " + x) })
        }

        val output = process.inputStream.bufferedReader().readText()
        log.info({ "Started instance '$instance' of '$agent' as '${output.trim()}'" })
    }

    suspend fun stopInstance(instance: String) {
        log.debug({ "Stopping instance $instance" })
        val dockerSearch = ProcessBuilder(
            "docker", "ps", "-q",
            "-f", "label=com.obecto.perper.instance=" + instance
        )
            .redirectOutput(Redirect.PIPE)
            .redirectError(if (log.isDebugEnabled()) Redirect.PIPE else Redirect.DISCARD)
            .start()

        if (log.isDebugEnabled()) {
            dockerSearch.errorStream.bufferedReader().lines().forEachOrdered({ x -> log.debug("docker ps: " + x) })
        }

        val containerIds = dockerSearch.inputStream.bufferedReader().lines().asSequence().toList().toTypedArray()
        log.debug({ "Stopping container ${containerIds.joinToString()}" })

        val dockerStop = ProcessBuilder("docker", "stop", *containerIds)
            .redirectOutput(if (log.isDebugEnabled()) Redirect.PIPE else Redirect.DISCARD)
            .redirectErrorStream(true)
            .start()

        if (log.isDebugEnabled()) {
            dockerStop.inputStream.bufferedReader().lines().forEachOrdered({ x -> log.debug("docker stop: " + x) })
        }

        log.info({ "Stopped instance $instance" })
    }
}
