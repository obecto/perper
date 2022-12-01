package com.obecto.perper.model
import kotlinx.coroutines.flow.Flow

data class PerperStreamItemFilter(
    val stream: PerperStream,
    val startFromListener: String? = null,
    val startFromLatest: Boolean = false,
    val localToData: Boolean = false,
)

interface PerperStreams {
    suspend fun create(ephemeral: Boolean = false, cacheOptions: CacheOptions, stream: PerperStream? = null): PerperStream

    suspend fun waitForListener(stream: PerperStream)

    suspend fun writeItem(stream: PerperStream, item: IgniteAny, key: Long? = null)

    fun listenItems(filter: PerperStreamItemFilter): Flow<Pair<Long, IgniteAny>>
    suspend fun updateListener(stream: PerperStream, listener: String, key: Long?)

    suspend fun delete(stream: PerperStream)
}
