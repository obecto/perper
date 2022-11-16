package com.obecto.perper.fabric
import com.obecto.perper.model.CacheOptions
import com.obecto.perper.model.PerperList
import com.obecto.perper.model.PerperListLocation
import com.obecto.perper.model.PerperListOperation
import com.obecto.perper.model.PerperLists
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.collect
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
import kotlin.math.max
import kotlin.math.min

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
class PerperListsIgniteImpl(val ignite: Ignite) : PerperLists {
    val binary = ignite.binary()
    val log = ignite.log().getLogger(this)

    // val statesCache = ignite.getOrCreateCache<String, Any>("states")

    // TODO: This next two should use just .cache and rely on create having been called, but we are hacking it this way while the C# client has GetInstanceChildrenList
    fun listCache(list: PerperList) = ignite.getOrCreateCache<Long, Any>(list.cacheName())
    fun listConfigurationCache(list: PerperList) = ignite.getOrCreateCache<String, Long>(list.configurationCacheName())

    fun PerperList.cacheName() = list
    fun PerperList.configurationCacheName() = list

    val startRawIndexKey = "start_index"; // Stores the index of the first item in the list, inclusive. Default to 0
    val endRawIndexKey = "end_index"; // Stores the index of the last item in the list, inclusive. Default to 0

    override suspend fun create(cacheOptions: CacheOptions, list: PerperList?): PerperList {
        val listNonNull = list ?: PerperList("${UUID.randomUUID()}")
        ignite.createCache(cacheOptions.toCacheConfiguration<String, Any>(listNonNull.cacheName()))
        return listNonNull
    }

    override suspend fun countItems(list: PerperList): Int {
        // return listCache().sizeAsync().await() - 2

        val configurationCache = listConfigurationCache(list)

        return ((configurationCache.getAsync(endRawIndexKey).await() ?: 0) - (configurationCache.getAsync(startRawIndexKey).await() ?: 0)).toInt()
    }

    override suspend fun operateItem(list: PerperList, location: PerperListLocation, operation: PerperListOperation): List<Any>? {
        val cache = listCache(list)
        val configurationCache = listConfigurationCache(list)

        val finalRawIndexKey = if (!location.backwards) { startRawIndexKey } else { endRawIndexKey }
        val step = if (!location.backwards) { 1 } else { -1 }

        // NOTE: Main loop explanation:
        // Starting state:
        // - cache: a cache of items already in the stream, e.g. {3: A, 4: B, 5: C}
        // - rawIndex: a location in that cache, e.g. 4
        // - shiftQueue: a queue of items to insert, e.g. [D, E]
        // - valuesToRemove: a count of items to remove, e.g. 1
        // - resultsList/valuesToGet: a capped empty list that will hold any items we've collected, e.g. []/1
        // For each step of the algorithm, we:
        // 1. Get the current item in the list, try to put it in the results list, e.g. get B and store it as a result
        // 2. Try to insert the next item in the shiftQueue in it's place, e.g. swap 4: B for 4: D
        // 3. Either remove/drop that item, or put it at the end of the shiftQueue, e.g. ignore B; store C
        // 4. Advance to the next location, e.g. move through 4->5->6
        // (e.g. intermediary cache/shiftQueue values: {3: A, _4_: B, 5: C}/[D, E] -> {3: A, 4: D, _5_: C}/[E] ->
        //   -> {3: A, 4: D, 5: E, _6_:}/[C] -> {3: A, 4: D, 5: E, 6: C}/[] )
        // End state:
        // - cache: a cache of items with the changes, e.g. {3: A, 4: D, 5: E, 6: C}
        // - shiftQueue: []
        // - valuesToRemove: 0
        // - resultsList/valuesToGet: [..]/0, e.g. [B]/0

        var rawIndex = if (location.rawIndex != null) {
            location.rawIndex
        } else if (location.index != null) {
            location.index + (configurationCache.getAsync(startRawIndexKey).await() ?: 0)
        } else { // End
            configurationCache.getAsync(finalRawIndexKey).await() ?: 0
        }

        val shiftQueue = ArrayDeque(operation.insertValues)
        var valuesToRemove = if (operation.removeValues) { operation.valuesCount } else { 0 }

        val resultsList = if (operation.getValues) { ArrayList<Any>(operation.valuesCount.toInt()) } else { null }
        var valuesToGet = if (operation.getValues) { operation.valuesCount } else { 0 }

        val lengthDifference = valuesToRemove - shiftQueue.size

        while (!shiftQueue.isEmpty() || valuesToRemove > 0 || valuesToGet > 0) {
            val replacementValue = shiftQueue.removeFirstOrNull()
            val value = if (replacementValue != null) {
                cache.getAndReplaceAsync(rawIndex, replacementValue).await()
            } else {
                cache.getAsync(rawIndex).await()
            }
            rawIndex += step

            if (value != null) {
                if (resultsList != null && valuesToGet > 0) {
                    valuesToGet -= 1
                    resultsList.add(value)
                }

                if (valuesToRemove > 0) {
                    valuesToRemove -= 1
                } else {
                    shiftQueue.addLast(value)
                }
            } else {
                // Either we are past the end or an botched operation left a hole in the list
                // Break only if we have no insertions left and we've indeed gone past the end
                if (shiftQueue.isEmpty() &&
                    rawIndex * step > (configurationCache.getAsync(finalRawIndexKey).await() ?: 0) * step
                ) {
                    break
                }
            }
        }

        // NOTE: Condition explanation:
        // If we've inserted items going forward, we want to move endRawIndexKey to a larger value, so we use max.
        // If we've removed items going forward, we want to move endRawIndexKey to an smaller value, so we use min.
        // If we've inserted items going backward, we want to move startRawIndexKey to a smaller value, so we use min.
        // If we've removed items going backward, we want to move startRawIndexKey to an larger value, so we use max.
        // That's equivalent to lengthDifference (+ if added, - if removed) * step (+ if forward, - if backward) being positive
        if (lengthDifference * step > 0) {
            configurationCache.optimisticUpdateSuspend(finalRawIndexKey, { max(rawIndex, (it ?: 0)) })
        } else if (lengthDifference * step < 0) {
            configurationCache.optimisticUpdateSuspend(finalRawIndexKey, { min(rawIndex, (it ?: 0)) })
        }

        return resultsList
    }

    override suspend fun locateItem(list: PerperList, value: Any): PerperListLocation? {
        val cache = listCache(list)
        val configurationCache = listConfigurationCache(list)

        val startRawIndex = configurationCache.getAsync(startRawIndexKey).await() ?: 0
        var rawIndex = startRawIndex
        while (true) {
            val currentValue = cache.getAsync(rawIndex).await()
            if (currentValue != null) {
                if (value == currentValue) {
                    return PerperListLocation((rawIndex - startRawIndex).toInt(), rawIndex)
                }
            } else if (rawIndex > (configurationCache.getAsync(endRawIndexKey).await() ?: 0)) {
                return null
            }
            rawIndex += 1
        }
    }

    override fun listItems(list: PerperList) = flow<Pair<PerperListLocation, Any>> {
        val startRawIndex = listConfigurationCache(list).getAsync(startRawIndexKey).await() ?: 0
        listCache(list).iterateQuery(ScanQuery<Long, Any>()).collect { pair ->
            emit(Pair(PerperListLocation((pair.first - startRawIndex).toInt(), pair.first), pair.second))
        }
    }

    override fun sqlQuery(list: PerperList, sql: String) = flow<List<Any?>> {
        val queryCursor = listCache(list).query(SqlFieldsQuery(sql))
        try {
            for (row in queryCursor) {
                emit(row)
            }
        } finally {
            queryCursor.close()
        }
    }

    override suspend fun deleteItems(list: PerperList) {
        listCache(list).removeAllAsync().await()
    }

    override suspend fun delete(list: PerperList) {
        withContext(Dispatchers.IO) {
            listCache(list).destroy()
        }
    }
}
