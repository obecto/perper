@file:JvmName("Main")

package com.obecto.perper.fabric
import kotlinx.cli.ArgParser
import kotlinx.cli.ArgType
import kotlinx.cli.default
import org.apache.ignite.Ignition
import org.apache.ignite.binary.BinaryBasicNameMapper
import org.apache.ignite.binary.BinaryTypeConfiguration
import org.apache.ignite.cache.CacheKeyConfiguration
import org.apache.ignite.configuration.BinaryConfiguration
import org.apache.ignite.configuration.ClientConnectorConfiguration
import org.apache.ignite.configuration.IgniteConfiguration
import org.apache.ignite.logger.slf4j.Slf4jLogger
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceConfiguration
import org.apache.ignite.spi.discovery.tcp.TcpDiscoverySpi
import org.apache.ignite.spi.discovery.tcp.ipfinder.multicast.TcpDiscoveryMulticastIpFinder

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
    val ignitePort by parser.option(ArgType.Int, "ignite-port", description = "Ignite client port").default(10800)
    val grpcPort by parser.option(ArgType.Int, "grpc-port", description = "Transport service port").default(40400)
    val noDiscovery by parser.option(ArgType.Boolean, "no-discovery", description = "Disable discovery").default(false)

    parser.parse(args)

    System.setProperty("org.slf4j.simpleLogger.defaultLogLevel", if (verbose) "info" else "warn")
    System.setProperty("org.slf4j.simpleLogger.log.com.obecto.perper", if (debug) "debug" else "info")
    System.setProperty("org.slf4j.simpleLogger.levelInBrackets", "true")
    System.setProperty("org.slf4j.simpleLogger.showDateTime", "true")

    val igniteConfiguration = IgniteConfiguration().also {
        it.clientConnectorConfiguration = ClientConnectorConfiguration().also {
            it.port = ignitePort
        }
        if (noDiscovery) {
            it.discoverySpi = TcpDiscoverySpi().also {
                it.ipFinder = TcpDiscoveryMulticastIpFinder().setMulticastPort(ignitePort)
            }
        }
        it.binaryConfiguration = BinaryConfiguration().also {
            it.typeConfigurations = listOf(
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.CallData>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.StreamData>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.StreamDelegateType>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.StreamListener>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.CallResultNotification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.CallTriggerNotification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.Notification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.NotificationKey>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.NotificationKeyLong>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.NotificationKeyString>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.StreamItemNotification>(),
                BinaryTypeConfiguration<com.obecto.perper.fabric.cache.notification.StreamTriggerNotification>(),
            )
            it.serializer = PerperBinarySerializer()
            it.nameMapper = BinaryBasicNameMapper(true)
        }
        it.setCacheKeyConfiguration(
            CacheKeyConfiguration(com.obecto.perper.fabric.cache.notification.NotificationKeyLong::class.java),
            CacheKeyConfiguration(com.obecto.perper.fabric.cache.notification.NotificationKeyString::class.java)
        )
        it.setServiceConfiguration(
            singletonServiceConfiguration("CallService", CallService()),
            singletonServiceConfiguration("StreamService", StreamService()),
            singletonServiceConfiguration("TransportService", TransportService(grpcPort)),
        )
        it.gridLogger = Slf4jLogger()
    }

    Ignition.start(igniteConfiguration)
}
