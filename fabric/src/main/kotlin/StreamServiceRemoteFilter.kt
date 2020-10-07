package com.obecto.perper.fabric
import org.apache.ignite.Ignite
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.resources.IgniteInstanceResource
import javax.cache.event.CacheEntryEvent
import javax.cache.event.CacheEntryEventFilter

public class StreamServiceRemoteFilter(val streamName: String) : CacheEntryEventFilter<Long, BinaryObject> {
    @set:IgniteInstanceResource
    lateinit var ignite: Ignite

    override fun evaluate(event: CacheEntryEvent<out Long, out BinaryObject>): Boolean {
        var service = ignite.services().service<StreamService>("StreamService")
        service.updateStreamItem(streamName, event.key)
        return false
    }
}
