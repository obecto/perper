@file:OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
package com.obecto.perper.fabric
import com.google.protobuf.Empty
import com.obecto.perper.model.PerperDictionaries
import com.obecto.perper.model.PerperDictionaryOperation
import com.obecto.perper.protobuf2.FabricStatesDictionaryGrpcKt
import com.obecto.perper.protobuf2.StatesDictionaryCountItemsRequest
import com.obecto.perper.protobuf2.StatesDictionaryCountItemsResponse
import com.obecto.perper.protobuf2.StatesDictionaryCreateRequest
import com.obecto.perper.protobuf2.StatesDictionaryDeleteRequest
import com.obecto.perper.protobuf2.StatesDictionaryListItemsRequest
import com.obecto.perper.protobuf2.StatesDictionaryListItemsResponse
import com.obecto.perper.protobuf2.StatesDictionaryOperateRequest
import com.obecto.perper.protobuf2.StatesDictionaryOperateResponse
import com.obecto.perper.protobuf2.StatesDictionaryQuerySQLRequest
import com.obecto.perper.protobuf2.StatesDictionaryQuerySQLResponse
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

private fun String.toNullIfEmpty(): String? = if (this.isEmpty()) { null } else { this }

@OptIn(kotlinx.coroutines.ExperimentalCoroutinesApi::class)
class Grpc2FabricStatesDictionaryImpl(
    val perperDictionaries: PerperDictionaries,
    val perperProtobufDescriptors: PerperProtobufDescriptors
) : FabricStatesDictionaryGrpcKt.FabricStatesDictionaryCoroutineImplBase() {

    // val log = ignite.log().getLogger(this)

    override suspend fun create(request: StatesDictionaryCreateRequest): Empty {
        perperDictionaries.create(
            dictionary = request.dictionary,
            cacheOptions = request.cacheOptions
        )
        return Empty.getDefaultInstance()
    }

    override suspend fun operate(request: StatesDictionaryOperateRequest): StatesDictionaryOperateResponse {
        val (success, value) = perperDictionaries.operateItem(
            dictionary = request.dictionary,
            key = perperProtobufDescriptors.unpack(request.key),
            operation = PerperDictionaryOperation(
                getValue = request.getExistingValue,
                setValue = Pair(request.setNewValue, perperProtobufDescriptors.unpack(request.newValue)),
                compareValue = Pair(request.compareExistingValue, perperProtobufDescriptors.unpack(request.expectedExistingValue)),
            )
        )
        return StatesDictionaryOperateResponse.newBuilder().also {
            it.operationSuccessful = success
            if (value != null) {
                it.previousValue = perperProtobufDescriptors.pack(value)
            }
        }.build()
    }

    override fun listItems(request: StatesDictionaryListItemsRequest): Flow<StatesDictionaryListItemsResponse> {
        return perperDictionaries.listItems(dictionary = request.dictionary).map { item ->
            StatesDictionaryListItemsResponse.newBuilder().also {
                it.key = perperProtobufDescriptors.pack(item.first)
                it.value = perperProtobufDescriptors.pack(item.second)
            }.build()
        }
    }

    override suspend fun countItems(request: StatesDictionaryCountItemsRequest): StatesDictionaryCountItemsResponse {
        val count = perperDictionaries.countItems(dictionary = request.dictionary)
        return StatesDictionaryCountItemsResponse.newBuilder().also {
            it.count = count
        }.build()
    }

    override fun querySQL(request: StatesDictionaryQuerySQLRequest): Flow<StatesDictionaryQuerySQLResponse> {
        return perperDictionaries.sqlQuery(
            dictionary = request.dictionary,
            sql = request.sql,
        ).map { values ->
            StatesDictionaryQuerySQLResponse.newBuilder().also {
                it.addAllSqlResultValues(values.filterNotNull().map { perperProtobufDescriptors.pack(it) })
            }.build()
        }
    }

    override suspend fun delete(request: StatesDictionaryDeleteRequest): Empty {
        if (request.keepCache) {
            perperDictionaries.deleteItems(dictionary = request.dictionary)
        } else {
            perperDictionaries.delete(dictionary = request.dictionary)
        }
        return Empty.getDefaultInstance()
    }
}
