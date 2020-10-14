package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryMapFactory
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable
import java.sql.Timestamp

class StreamData(
    var name: String,
    var delegate: String,
    var delegateType: StreamDelegateType,
    var streamParams: Map<String, List<StreamParam>>,
    var indexType: String?,
    var indexFields: LinkedHashMap<String, String>?,
    var workers: Map<String, WorkerData>,
) : Binarylizable {
    var lastModified: Timestamp = Timestamp(System.currentTimeMillis())

    override fun writeBinary(writer: BinaryWriter) {
        throw RuntimeException("Did not expect call to writeBinary")
    }

    override fun readBinary(reader: BinaryReader) {
        delegate = reader.readString("delegate")
        delegateType = reader.readEnum("delegateType")
        indexFields = reader.readMap<String, String>("indexFields", BinaryMapFactory { LinkedHashMap(it) }) as LinkedHashMap<String, String>?
        indexType = reader.readString("indexType")
        lastModified = reader.readTimestamp("lastModified")
        name = reader.readString("name")
        streamParams = reader.readMap<String, Array<StreamParam>>("streamParams").entries.associateBy({ it.key }, { it.value.toList() })
        workers = reader.readMap("workers")
    }
}
