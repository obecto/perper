package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.resources.ServiceResource
import javax.cache.configuration.Factory
import javax.cache.event.CacheEntryEventFilter

public class StreamServiceRemoteFilterFactory(val streamName: String) : Factory<CacheEntryEventFilter<Long, BinaryObject>> {
    @set:ServiceResource(serviceName = "StreamService")
    var streamService: StreamService = null!! // We expect Ignite to replace the null value before we use it

    override fun create() = CacheEntryEventFilter<Long, BinaryObject> { event ->
        streamService.updateStreamItem(streamName, event.key)
        false
    }
}
