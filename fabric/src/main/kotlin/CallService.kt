package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.CallData
import com.obecto.perper.fabric.cache.notification.CallResultNotification
import com.obecto.perper.fabric.cache.notification.CallTriggerNotification
import com.obecto.perper.fabric.cache.notification.NotificationKey
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import javax.cache.event.CacheEntryUpdatedListener

class CallService : JobService() {
    lateinit var log: IgniteLogger

    lateinit var ignite: Ignite

    lateinit var callsCache: IgniteCache<String, CallData>

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
        callsCache = ignite.getOrCreateCache("calls")

        super.init(ctx)
    }

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        var streamGraphUpdates = Channel<Pair<String, CallData>>(Channel.UNLIMITED)
        val query = ContinuousQuery<String, CallData>()
        query.localListener = CacheEntryUpdatedListener { events ->
            for (event in events) {
                if (event.isOldValueAvailable && event.oldValue.finished == event.value.finished) {
                    continue
                }
                runBlocking { streamGraphUpdates.send(Pair(event.key, event.value)) }
            }
        }
        callsCache.query(query)
        log.debug({ "Call listener started!" })
        for ((call, callData) in streamGraphUpdates) {
            log.debug({ "Call object modified '$call'" })
            updateCall(call, callData)
        }
    }

    suspend fun updateCall(call: String, callData: CallData) {
        if (callData.finished) {
            if (callData.callerAgentDelegate == "") return

            val notificationsCache = TransportService.getNotificationCache(ignite, callData.callerAgentDelegate)
            val key = NotificationKey(TransportService.getCurrentTicks(), if (callData.localToData) call else callData.caller)
            notificationsCache.put(key, CallResultNotification(call, callData.caller))
        } else {
            val notificationsCache = TransportService.getNotificationCache(ignite, callData.agentDelegate)
            val key = NotificationKey(TransportService.getCurrentTicks(), call)
            notificationsCache.put(key, CallTriggerNotification(call, callData.delegate))
        }
    }
}
