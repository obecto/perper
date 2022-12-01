package com.obecto.perper.model
import kotlinx.coroutines.flow.Flow

data class PerperDictionaryOperation(
    val getValue: Boolean = false,
    val setValue: Pair<Boolean, IgniteAny?> = Pair(false, null), // Actually an Option<IgniteAny?> in disguise
    val compareValue: Pair<Boolean, IgniteAny?> = Pair(false, null),
)

interface PerperDictionaries {
    suspend fun create(cacheOptions: CacheOptions, dictionary: PerperDictionary? = null): PerperDictionary

    suspend fun operateItem(dictionary: PerperDictionary, key: IgniteAny, operation: PerperDictionaryOperation): Pair<Boolean, IgniteAny?>

    fun listItems(dictionary: PerperDictionary): Flow<Pair<IgniteAny, IgniteAny>>

    suspend fun countItems(dictionary: PerperDictionary): Int

    fun sqlQuery(dictionary: PerperDictionary, sql: String): Flow<List<IgniteAny?>>

    suspend fun deleteItems(dictionary: PerperDictionary)

    suspend fun delete(dictionary: PerperDictionary)
}
