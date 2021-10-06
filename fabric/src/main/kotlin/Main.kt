@file:JvmName("Main")

package com.obecto.perper.fabric
import kotlinx.cli.ArgParser
import kotlinx.cli.ArgType
import kotlinx.cli.default
import org.apache.ignite.Ignition
import org.apache.ignite.cluster.ClusterState

fun main(args: Array<String>) {
    val parser = ArgParser("perper-fabric")

    val debug by parser.option(ArgType.Boolean, shortName = "d", description = "Show debug logs").default(false)
    val trace by parser.option(ArgType.Boolean, description = "Show trace logs").default(false)
    val verbose by parser.option(ArgType.Boolean, shortName = "v", description = "Show Ignite information logs").default(false)
    val activate by parser.option(ArgType.Boolean, description = "Set cluster state to active on startup").default(true)
    val configXml by parser.argument(ArgType.String, description = "Path to Spring configuration XML")

    parser.parse(args)

    System.setProperty("org.slf4j.simpleLogger.defaultLogLevel", if (verbose) "info" else "warn")
    System.setProperty("org.slf4j.simpleLogger.log.com.obecto.perper", if (trace) "trace" else if (debug) "debug" else "info")
    System.setProperty("org.slf4j.simpleLogger.levelInBrackets", "true")
    System.setProperty("org.slf4j.simpleLogger.showDateTime", "true")

    val ignite = Ignition.start(configXml)
    if (activate) {
        ignite.cluster().state(ClusterState.ACTIVE)
    }
}
