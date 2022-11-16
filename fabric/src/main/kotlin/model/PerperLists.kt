package com.obecto.perper.model
import kotlinx.coroutines.flow.Flow

data class PerperListOperation(
    val valuesCount: Long,
    val getValues: Boolean = false,
    val removeValues: Boolean = false,
    val insertValues: List<Any?> = emptyList(),
)

data class PerperListLocation(
    val index: Int?,
    val rawIndex: Long? = null,
    val backwards: Boolean = false,
)

interface PerperLists {
    suspend fun create(cacheOptions: CacheOptions, list: PerperList? = null): PerperList

    suspend fun countItems(list: PerperList): Int

    suspend fun operateItem(list: PerperList, location: PerperListLocation, operation: PerperListOperation): List<Any>?

    suspend fun locateItem(list: PerperList, value: Any): PerperListLocation?

    fun listItems(list: PerperList): Flow<Pair<PerperListLocation, Any>>

    fun sqlQuery(list: PerperList, sql: String): Flow<List<Any?>>

    suspend fun deleteItems(list: PerperList)

    suspend fun delete(list: PerperList)
}
