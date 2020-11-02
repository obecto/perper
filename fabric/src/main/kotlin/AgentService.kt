package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.AgentData
import com.obecto.perper.fabric.cache.notification.Notification
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.cache.affinity.AffinityKey
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import javax.cache.CacheException
import javax.cache.event.CacheEntryUpdatedListener

class AgentService : JobService() {
    @set:LoggerResource
    lateinit var log: IgniteLogger

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    lateinit var agentsCache: IgniteCache<String, AgentData>

    override fun init(ctx: ServiceContext) {
        agentsCache = ignite.getOrCreateCache("agents")

        super.init(ctx)
    }

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        var agentsUpdates = Channel<Pair<String, AgentData>>(Channel.UNLIMITED)
        val query = ContinuousQuery<String, AgentData>()
        query.localListener = CacheEntryUpdatedListener { events ->
            for (event in events) {
                runBlocking { agentsUpdates.send(Pair(event.key, event.value)) }
            }
        }
        agentsCache.query(query)
        log.debug({ "Agents listener started!" })
        for ((agent, agentData) in agentsUpdates) {
            log.debug({ "Agent object modified '$agent'" })
            updateAgent(agent, agentData)
        }
    }

    suspend fun updateAgent(@Suppress("UNUSED_PARAMETER") agent: String, agentData: AgentData) {
        val notificationsCacheName = "${agentData.delegate}-\$notifications"
        if (ignite.cacheNames().contains(notificationsCacheName)) return
        log.debug({ "Creating cache '$notificationsCacheName'" })

        try {
            ignite.createCache<AffinityKey<Long>, Notification>(notificationsCacheName)
        } catch (_: CacheException) {
            // Likely already created
        }
    }

    fun getNotificationCache(agentDelegate: String) = ignite.cache<AffinityKey<Long>, Notification>("$agentDelegate-\$notifications")
}
