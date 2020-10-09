@file:JvmName("Main")

package com.obecto.perper.fabric
import org.apache.ignite.Ignition
import org.apache.ignite.binary.BinaryBasicNameMapper
import org.apache.ignite.binary.BinaryReflectiveSerializer
import org.apache.ignite.binary.BinaryTypeConfiguration
import org.apache.ignite.configuration.BinaryConfiguration
import org.apache.ignite.configuration.IgniteConfiguration

fun main() {
//     System.setProperty("IGNITE_QUIET", "false")

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
    }
    val ignite = Ignition.start(cfg)

    ignite.services().deployNodeSingleton("TransportService", TransportService(40400))
    ignite.services().deployNodeSingleton("StreamService", StreamService())
}
