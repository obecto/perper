@file:JvmName("Main")

package com.obecto.perper.fabric
import org.apache.ignite.IgniteCache
import org.apache.ignite.Ignition
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.configuration.IgniteConfiguration
import javax.cache.event.CacheEntryUpdatedListener

fun main() {
    val cfg = IgniteConfiguration()
    val ignite = Ignition.start(cfg)

    val streams: IgniteCache<String, StreamData> = ignite.getOrCreateCache("streams")

    ignite.services().deployNodeSingleton("TransportService", TransportService())
    ignite.services().deployNodeSingleton("StreamService", StreamService())

    val streamService = ignite.services().service<StreamService>("StreamService")

    val query = ContinuousQuery<String, StreamData>()
    query.localListener = CacheEntryUpdatedListener { events ->
        for (event in events) {
            streamService.updateStream(event.value)
        }
    }

    streams.query(query)
}
