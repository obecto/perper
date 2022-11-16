package com.obecto.perper.fabric
import com.obecto.perper.model.CacheOptions
import com.obecto.perper.model.PerperDictionaries
import com.obecto.perper.model.PerperDictionary
import com.obecto.perper.model.PerperDictionaryOperation
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.flow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.withContext
import org.apache.ignite.Ignite
import org.apache.ignite.cache.QueryEntity
import org.apache.ignite.cache.query.ScanQuery
import org.apache.ignite.cache.query.SqlFieldsQuery
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
class PerperDictionariesIgniteImpl(val ignite: Ignite) : PerperDictionaries {
    val binary = ignite.binary()
    val log = ignite.log().getLogger(this)

    // val statesCache = ignite.getOrCreateCache<String, Any>("states")
    fun dictionaryCache(dictionary: PerperDictionary) = ignite.getOrCreateCache<Any, Any>(dictionary.cacheName()) // TODO: This should be just .cache and rely on create having been called, but we are hacking it this way while the C# client has GetInstanceDictionary

    fun PerperDictionary.cacheName() = dictionary

    override suspend fun create(cacheOptions: CacheOptions, dictionary: PerperDictionary?): PerperDictionary {
        val dictionaryNonNull = dictionary ?: PerperDictionary("${UUID.randomUUID()}")
        ignite.createCache(cacheOptions.toCacheConfiguration<String, Any>(dictionaryNonNull.cacheName()))
        return dictionaryNonNull
    }

    override suspend fun operateItem(dictionary: PerperDictionary, key: Any, operation: PerperDictionaryOperation): Pair<Boolean, Any?> {
        val cache = dictionaryCache(dictionary)
        if (!operation.getValue) {
            if (!operation.setValue.first) {
                if (!operation.compareValue.first) {
                    return Pair(cache.containsKeyAsync(key).await()!!, null)
                } else {
                    return Pair(cache.getAsync(key).await() == operation.compareValue.second, null)
                }
            } else {
                if (!operation.compareValue.first) {
                    return Pair(cache.putOrRemoveSuspend(key, operation.setValue.second), null)
                } else {
                    return Pair(cache.replaceOrRemoveSuspend(key, operation.compareValue.second, operation.setValue.second), null)
                }
            }
        } else {
            if (!operation.setValue.first) {
                val value = cache.getAsync(key).await()
                if (operation.compareValue.first) {
                    return Pair(operation.compareValue.second == value, value)
                } else {
                    return Pair(true, value)
                }
            } else {
                if (operation.compareValue.first) {
                    if (operation.compareValue.second != null) {
                        while (true) {
                            val value = cache.getAsync(key).await()
                            if (value != operation.compareValue.second) {
                                return Pair(false, value)
                            }
                            if (cache.replaceOrRemoveSuspend(key, value, operation.setValue.second)) {
                                return Pair(true, value)
                            }
                        }
                    } else {
                        return Pair(true, cache.getAndPutIfAbsentAsync(key, operation.setValue.second).await()) // TODO: "true" is wrong here
                    }
                } else {
                    return Pair(true, cache.getAndPutOrRemoveSuspend(key, operation.setValue.second))
                }
            }
        }
    }

    override suspend fun countItems(dictionary: PerperDictionary): Int {
        return dictionaryCache(dictionary).sizeAsync().await()!!
    }

    override fun listItems(dictionary: PerperDictionary): Flow<Pair<Any, Any>> {
        return dictionaryCache(dictionary).iterateQuery(ScanQuery<Any, Any>())
    }

    override fun sqlQuery(dictionary: PerperDictionary, sql: String) = flow<List<Any?>> {
        val queryCursor = dictionaryCache(dictionary).query(SqlFieldsQuery(sql))
        try {
            for (row in queryCursor) {
                emit(row)
            }
        } finally {
            queryCursor.close()
        }
    }

    override suspend fun deleteItems(dictionary: PerperDictionary) {
        dictionaryCache(dictionary).removeAllAsync().await()
    }

    override suspend fun delete(dictionary: PerperDictionary) {
        withContext(Dispatchers.IO) {
            dictionaryCache(dictionary).destroy()
        }
    }
}
