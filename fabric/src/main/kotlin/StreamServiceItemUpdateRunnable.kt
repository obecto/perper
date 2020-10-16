package com.obecto.perper.fabric
import org.apache.ignite.Ignite
import org.apache.ignite.lang.IgniteRunnable
import org.apache.ignite.resources.IgniteInstanceResource

public class StreamServiceItemUpdateRunnable(
    val receiverStream: String,
    val receiverDelegate: String,
    val receiverParameter: String,
    val senderStream: String,
    val senderItem: Long
) : IgniteRunnable {
    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    override fun run() {
        var transportService = ignite.services().service<TransportService>("TransportService")
        transportService.sendItemUpdate(receiverStream, receiverDelegate, receiverParameter, senderStream, senderItem)
    }
}
