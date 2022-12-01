package com.obecto.perper.fabric
import com.obecto.perper.fabric.cache.StreamData
import com.obecto.perper.fabric.cache.StreamListener
import com.obecto.perper.model.CacheOptions
import com.obecto.perper.model.IgniteAny
import com.obecto.perper.model.PerperStream
import com.obecto.perper.model.PerperStreamItemFilter
import com.obecto.perper.model.PerperStreams
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.filter
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.withContext
import org.apache.ignite.Ignite
import org.apache.ignite.cache.QueryEntity
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.configuration.CacheConfiguration
import java.util.UUID

private fun <K, V> CacheOptions.toCacheConfiguration(name: String) = CacheConfiguration<K, V>(name).also {
    it.queryEntities = indexTypeUrlsList.map { typeUrl ->
        QueryEntity().also {
            it.valueType = typeUrl // TODO
        }
    }
    if (!dataRegion.isNullOrEmpty()) {
        it.dataRegionName = dataRegion
    }
}

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
class PerperStreamsIgniteImpl(val ignite: Ignite) : PerperStreams {
    val binary = ignite.binary()
    val log = ignite.log().getLogger(this)

    val streamsCache = ignite.getOrCreateCache<String, StreamData>("streams")
    val streamListenersCache = ignite.getOrCreateCache<String, StreamListener>("stream-listeners")
    fun streamCache(stream: PerperStream) = ignite.cache<Long, Any>(stream.cacheName()).withKeepBinary<Long, Any>()

    fun PerperStream.cacheName() = stream
    fun streamListenerKey(stream: PerperStream, listener: String) = "${stream.stream}-$listener"

    override suspend fun create(ephemeral: Boolean, cacheOptions: CacheOptions, stream: PerperStream?): PerperStream {
        val streamNonNull = stream ?: PerperStream("${UUID.randomUUID()}")
        streamsCache.putAsync(
            streamNonNull.stream,
            binary.toBinary(
                StreamData(
                    ephemeral = ephemeral
                )
            )
        ).await()
        ignite.createCache(cacheOptions.toCacheConfiguration<Long, Any>(streamNonNull.cacheName()))
        return streamNonNull
    }

    override suspend fun waitForListener(stream: PerperStream) {
        val query = ContinuousQuery<String, StreamListener>()

        val streamKey = stream.stream
        query.setScanAndRemoteFilter { _, currentValue, oldValue ->
            currentValue != null && oldValue == null && currentValue.stream == streamKey
        }

        streamListenersCache.iterateQuery(query, Channel.CONFLATED).first()
    }

    override suspend fun writeItem(stream: PerperStream, item: IgniteAny, key: Long?) {
        val keyNonNull = key ?: Ticks.getCurrentTicks()
        streamCache(stream).putAsync(keyNonNull, item.wrappedValue).await()
    }

    override suspend fun delete(stream: PerperStream) {
        withContext(Dispatchers.IO) {
            streamCache(stream).destroy()
        }
    }

    override fun listenItems(filter: PerperStreamItemFilter): Flow<Pair<Long, IgniteAny>> = flow {
        val cache = streamCache(filter.stream)

        var startKey = filter.stream.startKey
        val stride = filter.stream.stride
        var startFromLatest = filter.startFromLatest

        if (filter.startFromListener != null) {
            val listener = streamListenersCache.getAsync(streamListenerKey(filter.stream, filter.startFromListener)).await()
            if (listener != null && listener.position > startKey) {
                startKey = listener.position
                startFromLatest = false
            }
        }

        if (stride == 0L) {
            val query = ContinuousQuery<Long, Any>()
            query.setLocal(filter.localToData)

            query.setScanAndRemoteFilter { key, currentValue, oldValue ->
                currentValue != null && oldValue == null && key >= startKey
            }

            if (startFromLatest) {
                query.initialQuery = null
            }

            var lastKey = startKey - 1 // -1 so that we still deliver the item at the start key
            cache.iterateQuery(query, sortKeysBy = compareBy { it }).collect { pair ->
                val (key, value) = pair

                if (key > lastKey) {
                    lastKey = key
                    emit(Pair(key, IgniteAny(value!!)))
                }
            }
        } else {
            var key = startKey

            if (startFromLatest) {
                val keys = cache.query(ScanQuery<Long, Any>()).map({ item -> item.key })
                val keyOrNull = keys.maxOrNull() // if (stride > 0) else keys.minOrNull()
                if (keyOrNull != null) {
                    key = keyOrNull
                } else {
                    key = cache.iterateQuery(ContinuousQuery<Long, Any>(), 1, BufferOverflow.DROP_LATEST).first().first
                }
            }

            while (true) {
                val value = cache.getWhenPredicateSuspend(key, localToData = filter.localToData) { it != null }!!

                emit(Pair(key, IgniteAny(value)))

                key += stride
            }
        }
    }

    override suspend fun updateListener(stream: PerperStream, listener: String, key: Long?) {
        if (key == null) {
            streamListenersCache.removeAsync(streamListenerKey(stream, listener)).await()
        } else {
            streamListenersCache.putAsync(streamListenerKey(stream, listener), StreamListener(stream.stream, key)).await()
        }
    }
}
