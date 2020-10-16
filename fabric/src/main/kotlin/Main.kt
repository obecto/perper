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
import org.apache.ignite.services.ServiceConfiguration
import org.apache.ignite.logger.slf4j.Slf4jLogger

fun main(args: Array<String>) {
    val parser = ArgParser("perper-fabric")
    val debug by parser.option(ArgType.Boolean, shortName = "d", description = "Show debug logs").default(false)
    val verbose by parser.option(ArgType.Boolean, shortName = "v", description = "Show Ignite information logs").default(false)
    parser.parse(args)

    System.setProperty("org.slf4j.simpleLogger.defaultLogLevel", if (verbose) "info" else "warn")
    System.setProperty("org.slf4j.simpleLogger.log.com.obecto.perper", if (debug) "debug" else "info")
    System.setProperty("org.slf4j.simpleLogger.levelInBrackets", "true")

    val cfg = IgniteConfiguration().also {
        it.binaryConfiguration = BinaryConfiguration().also {
            it.typeConfigurations = listOf(
                BinaryTypeConfiguration(StreamData::class.qualifiedName),
                BinaryTypeConfiguration(StreamParam::class.qualifiedName),
                BinaryTypeConfiguration(StreamDelegateType::class.qualifiedName).setEnum(true),
                BinaryTypeConfiguration(WorkerData::class.qualifiedName),
            )
            it.serializer = BinaryReflectiveSerializer()
            it.nameMapper = BinaryBasicNameMapper(true)
        }
        it.setServiceConfiguration(
            ServiceConfiguration().also {
                it.name = "TransportService"
                it.service = TransportService(40400)
                it.maxPerNodeCount = 1
                it.totalCount = 0
            },
            ServiceConfiguration().also {
                it.name = "StreamService"
                it.service = StreamService()
                it.maxPerNodeCount = 1
                it.totalCount = 0
            }
        )
        it.gridLogger = Slf4jLogger()
    }
    Ignition.start(cfg)
}
