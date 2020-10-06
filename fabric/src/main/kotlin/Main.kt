@file:JvmName("Main")

package com.obecto.perper.fabric
import org.apache.ignite.Ignition
import org.apache.ignite.configuration.IgniteConfiguration

fun main() {
    val cfg = IgniteConfiguration()
    val ignite = Ignition.start(cfg)

    ignite.services().deployNodeSingleton("TransportService", TransportService(40400))
    ignite.services().deployNodeSingleton("StreamService", StreamService())
}
