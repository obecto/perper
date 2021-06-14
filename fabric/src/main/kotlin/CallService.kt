package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.notification.CallResultNotification
import com.obecto.perper.fabric.cache.notification.CallTriggerNotification
import com.obecto.perper.fabric.cache.notification.Notification
import com.obecto.perper.fabric.cache.notification.NotificationKey
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteCache
import org.apache.ignite.IgniteLogger
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import javax.cache.event.CacheEntryUpdatedListener

class CallService : JobService() {
    lateinit var log: IgniteLogger

    lateinit var ignite: Ignite

    lateinit var callsCache: IgniteCache<String, BinaryObject>

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

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        launch { listenCache("calls") }
    }

    val BinaryObject.finished get() = field<Boolean>("finished")
    val BinaryObject.localToData get() = field<Boolean>("localToData")
    val BinaryObject.agent get() = field<String>("agent")
    val BinaryObject.delegate get() = field<String>("delegate")
    val BinaryObject.callerAgent get() = field<String>("callerAgent")
    val BinaryObject.caller get() = field<String>("caller")

    suspend fun CoroutineScope.listenCache(callsCacheName: String) {
        val callsCache = ignite.getOrCreateCache<String, Any>(callsCacheName).withKeepBinary<String, BinaryObject>()
        val query = ContinuousQuery<String, BinaryObject>()
        query.localListener = CacheEntryUpdatedListener { events ->
            for (event in events) {
                if (event.isOldValueAvailable && event.oldValue.finished == event.value.finished) {
                    continue
                }

                val call = event.key
                val callData = event.value
                log.debug({ "Call object modified $call" })

                val notifiedDelegate: String
                val notificationKey: NotificationKey
                val notification: Notification

                if (callData.finished) {
                    if (callData.callerAgent == "") {
                        continue
                    }
                    notifiedDelegate = callData.callerAgent
                    notificationKey = NotificationKey(TransportService.getCurrentTicks(), if (callData.localToData) call else callData.caller)
                    notification = CallResultNotification(call, callData.caller)
                } else {
                    notifiedDelegate = callData.agent
                    notificationKey = NotificationKey(TransportService.getCurrentTicks(), call)
                    notification = CallTriggerNotification(call, callData.delegate)
                }

                coroutineScope.launch {
                    val notificationsCache = TransportService.getNotificationCache(ignite, notifiedDelegate)
                    notificationsCache.put(notificationKey, notification)
                }
            }
        }
        callsCache.query(query)
        log.debug({ "Call listener started!" })
    }
}
