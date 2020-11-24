package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.CallData
import com.obecto.perper.fabric.cache.notification.CallResultNotification
import com.obecto.perper.fabric.cache.notification.CallTriggerNotification
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.cache.affinity.AffinityKey
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import javax.cache.event.CacheEntryUpdatedListener

class CallService(val startCall: Pair<String, String>?) : JobService() {
    val returnFieldName = "\$return"

    @set:LoggerResource
    lateinit var log: IgniteLogger

    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    lateinit var callsCache: IgniteCache<String, CallData>

    override fun init(ctx: ServiceContext) {
        callsCache = ignite.getOrCreateCache("calls")

        super.init(ctx)
    }

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        if (startCall != null) launch {
            val (startAgentDelegate, startCallDelegate) = startCall
            val agentName = startAgentDelegate + "-launchAgent"
            val callName = startCallDelegate + "-launchCall"
            callsCache.putIfAbsent(
                callName,
                CallData(
                    agent = agentName,
                    agentDelegate = startAgentDelegate,
                    delegate = startCallDelegate,
                    callerAgentDelegate = startAgentDelegate,
                    caller = "",
                    finished = false,
                    localToData = false
                )
            )
        }

        var streamGraphUpdates = Channel<Pair<String, CallData>>(Channel.UNLIMITED)
        val query = ContinuousQuery<String, CallData>()
        query.localListener = CacheEntryUpdatedListener { events ->
            for (event in events) {
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
            val notificationsCache = TransportService.getNotificationCache(ignite, callData.callerAgentDelegate)
            val key = AffinityKey(System.currentTimeMillis(), if (callData.localToData) call else callData.caller)
            notificationsCache.put(key, CallResultNotification(call, callData.caller))
        } else {
            val notificationsCache = TransportService.getNotificationCache(ignite, callData.agentDelegate)
            val key = AffinityKey(System.currentTimeMillis(), call)
            notificationsCache.put(key, CallTriggerNotification(call, callData.delegate))
        }
    }
}
