package com.obecto.perper.model
import kotlinx.coroutines.flow.Flow

data class PerperDictionaryOperation(
    val getValue: Boolean = false,
    val setValue: Pair<Boolean, Any?> = Pair(false, null), // Actually an Option<Any?> in disguise
    val compareValue: Pair<Boolean, Any?> = Pair(false, null),
)

interface PerperDictionaries {
    suspend fun create(cacheOptions: CacheOptions, dictionary: PerperDictionary? = null): PerperDictionary

    suspend fun operateItem(dictionary: PerperDictionary, key: Any, operation: PerperDictionaryOperation): Pair<Boolean, Any?>

    fun listItems(dictionary: PerperDictionary): Flow<Pair<Any, Any>>

    suspend fun countItems(dictionary: PerperDictionary): Int

    fun sqlQuery(dictionary: PerperDictionary, sql: String): Flow<List<Any?>>

    suspend fun deleteItems(dictionary: PerperDictionary)

    suspend fun delete(dictionary: PerperDictionary)
}
