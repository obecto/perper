@file:JvmName("Main")

package com.obecto.perper.fabric
import kotlinx.cli.ArgParser
import kotlinx.cli.ArgType
import kotlinx.cli.default
import org.apache.ignite.Ignition
import org.apache.ignite.binary.BinaryBasicNameMapper
import org.apache.ignite.binary.BinaryReflectiveSerializer
import org.apache.ignite.binary.BinaryTypeConfiguration
import org.apache.ignite.configuration.BinaryConfiguration
import org.apache.ignite.configuration.IgniteConfiguration
import org.apache.ignite.logger.slf4j.Slf4jLogger
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceConfiguration

private inline fun <reified T : Any> BinaryTypeConfiguration() =
    BinaryTypeConfiguration(T::class.qualifiedName).setEnum(T::class.java.isEnum())

private fun singletonServiceConfiguration(name: String, instance: Service) = ServiceConfiguration().also {
    it.name = name
    it.service = instance
    it.maxPerNodeCount = 1
    it.totalCount = 0
}

fun main(args: Array<String>) {
    val parser = ArgParser("perper-fabric")

    val debug by parser.option(ArgType.Boolean, shortName = "d", description = "Show debug logs").default(false)
    val verbose by parser.option(ArgType.Boolean, shortName = "v", description = "Show Ignite information logs").default(false)

    parser.parse(args)

    System.setProperty("org.slf4j.simpleLogger.defaultLogLevel", if (verbose) "info" else "warn")
    System.setProperty("org.slf4j.simpleLogger.log.com.obecto.perper", if (debug) "debug" else "info")
    System.setProperty("org.slf4j.simpleLogger.levelInBrackets", "true")

    val igniteConfiguration = IgniteConfiguration().also {
        it.binaryConfiguration = BinaryConfiguration().also {
            it.typeConfigurations = listOf(
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.AgentData>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.CallData>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.StreamData>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.StreamDelegateType>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.StreamListener>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.CallResultNotification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.CallTriggerNotification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.Notification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.StreamItemNotification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.StreamTriggerNotification>(),
            )
            it.serializer = BinaryReflectiveSerializer()
            it.nameMapper = BinaryBasicNameMapper(true)
        }
        it.setServiceConfiguration(
            singletonServiceConfiguration("CallService", CallService()),
            singletonServiceConfiguration("StreamService", StreamService()),
            singletonServiceConfiguration("TransportService", TransportService(40400)),
        )
        it.gridLogger = Slf4jLogger()
    }

    Ignition.start(igniteConfiguration)
}
