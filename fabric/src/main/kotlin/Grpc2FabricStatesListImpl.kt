@file:OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
package com.obecto.perper.fabric
import com.google.protobuf.Empty
import com.obecto.perper.model.PerperListLocation
import com.obecto.perper.model.PerperListOperation
import com.obecto.perper.model.PerperLists
import com.obecto.perper.protobuf2.FabricStatesListGrpcKt
import com.obecto.perper.protobuf2.StatesListCountItemsRequest
import com.obecto.perper.protobuf2.StatesListCountItemsResponse
import com.obecto.perper.protobuf2.StatesListCreateRequest
import com.obecto.perper.protobuf2.StatesListDeleteRequest
import com.obecto.perper.protobuf2.StatesListListItemsRequest
import com.obecto.perper.protobuf2.StatesListListItemsResponse
import com.obecto.perper.protobuf2.StatesListLocateRequest
import com.obecto.perper.protobuf2.StatesListLocateResponse
import com.obecto.perper.protobuf2.StatesListOperateRequest
import com.obecto.perper.protobuf2.StatesListOperateResponse
import com.obecto.perper.protobuf2.StatesListQuerySQLRequest
import com.obecto.perper.protobuf2.StatesListQuerySQLResponse
import kotlinx.coroutines.async
import kotlinx.coroutines.awaitAll
import kotlinx.coroutines.coroutineScope
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private fun String.toNullIfEmpty(): String? = if (this.isEmpty()) { null } else { this }

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
class Grpc2FabricStatesListImpl(
    val perperLists: PerperLists,
    val perperProtobufDescriptors: ProtobufConverter
) : FabricStatesListGrpcKt.FabricStatesListCoroutineImplBase() {

    // val log = ignite.log().getLogger(this)

    override suspend fun create(request: StatesListCreateRequest): Empty {
        perperLists.create(
            list = request.list,
            cacheOptions = request.cacheOptions
        )
        return Empty.getDefaultInstance()
    }

    override suspend fun operate(request: StatesListOperateRequest): StatesListOperateResponse {
        val result = perperLists.operateItem(
            list = request.list,
            location = when (request.locationCase) {
                StatesListOperateRequest.LocationCase.AT_FRONT -> PerperListLocation(index = null, backwards = false)
                StatesListOperateRequest.LocationCase.AT_BACK -> PerperListLocation(index = null, backwards = true)
                StatesListOperateRequest.LocationCase.INDEX -> PerperListLocation(index = request.index, backwards = false)
                StatesListOperateRequest.LocationCase.INDEX_BACKWARDS -> PerperListLocation(index = request.indexBackwards, backwards = true)
                StatesListOperateRequest.LocationCase.RAW_INDEX -> PerperListLocation(index = null, rawIndex = request.rawIndex, backwards = false)
                StatesListOperateRequest.LocationCase.RAW_INDEX_BACKWARDS -> PerperListLocation(index = null, rawIndex = request.rawIndexBackwards, backwards = true)
                else -> throw IllegalArgumentException("Location is required")
            },
            operation = PerperListOperation(
                valuesCount = request.valuesCount,
                getValues = request.getValues,
                removeValues = request.removeValues,
                insertValues = coroutineScope { request.insertValuesList.map { async { perperProtobufDescriptors.unpack(it)!! } }.awaitAll() },
            )
        )
        return StatesListOperateResponse.newBuilder().also {
            if (result != null) it.addAllValues(result.filterNotNull().map { perperProtobufDescriptors.pack(it) })
        }.build()
    }

    override suspend fun locate(request: StatesListLocateRequest): StatesListLocateResponse {
        val location = perperLists.locateItem(
            list = request.list,
            value = perperProtobufDescriptors.unpack(request.value)!!
        )
        return StatesListLocateResponse.newBuilder().also {
            if (location == null) {
                it.found = false
            } else {
                it.found = true
                if (location.index != null) it.index = location.index
                if (location.rawIndex != null) it.rawIndex = location.rawIndex
            }
        }.build()
    }

    override fun listItems(request: StatesListListItemsRequest): Flow<StatesListListItemsResponse> {
        return perperLists.listItems(list = request.list).map { item ->
            StatesListListItemsResponse.newBuilder().also {
                val (location, value) = item
                if (location.index != null) it.index = location.index
                if (location.rawIndex != null) it.rawIndex = location.rawIndex
                it.value = perperProtobufDescriptors.pack(value)
            }.build()
        }
    }

    override suspend fun countItems(request: StatesListCountItemsRequest): StatesListCountItemsResponse {
        val count = perperLists.countItems(list = request.list)
        return StatesListCountItemsResponse.newBuilder().also {
            it.count = count
        }.build()
    }

    override fun querySQL(request: StatesListQuerySQLRequest): Flow<StatesListQuerySQLResponse> {
        return perperLists.sqlQuery(
            list = request.list,
            sql = request.sql,
        ).map { values ->
            StatesListQuerySQLResponse.newBuilder().also {
                it.addAllSqlResultValues(values.filterNotNull().map { perperProtobufDescriptors.pack(it) })
            }.build()
        }
    }

    override suspend fun delete(request: StatesListDeleteRequest): Empty {
        if (request.keepCache) {
            perperLists.deleteItems(list = request.list)
        } else {
            perperLists.delete(list = request.list)
        }
        return Empty.getDefaultInstance()
    }
}
