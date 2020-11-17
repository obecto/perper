package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.AgentData
import kotlinx.coroutines.CoroutineScope
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext

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
    }
}
