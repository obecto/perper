package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryMapFactory
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable
import java.sql.Timestamp

class StreamData(
    var name: String,
    var delegate: String,
    var delegateType: StreamDelegateType,
    var params: BinaryObject?,
    var streamParams: Map<String, List<String>>,
    var indexType: String?,
    var indexFields: LinkedHashMap<String, String>?,
    var workers: Map<String, WorkerData>,
) : Binarylizable {
    var lastModified: Timestamp = Timestamp(System.currentTimeMillis())

    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("delegate", delegate)
        writer.writeEnum("delegateType", delegateType)
        writer.writeMap("indexFields", indexFields)
        writer.writeString("indexType", indexType)
        writer.writeTimestamp("lastModified", lastModified)
        writer.writeString("name", name)
        writer.writeObject("params", params)
        writer.writeMap("streamParams", streamParams.entries.associateBy({ it.key }, { it.value.toTypedArray() }))
        writer.writeMap("workers", workers)
    }

    override fun readBinary(reader: BinaryReader) {
        delegate = reader.readString("delegate")
        delegateType = reader.readEnum("delegateType")
        indexFields = reader.readMap<String, String>("indexFields", BinaryMapFactory { LinkedHashMap(it) }) as LinkedHashMap<String, String>?
        indexType = reader.readString("indexType")
        lastModified = reader.readTimestamp("lastModified")
        name = reader.readString("name")
        params = reader.readObject("params")
        streamParams = reader.readMap<String, Array<String>>("streamParams").entries.associateBy({ it.key }, { it.value.toList() })
        workers = reader.readMap("workers")
    }
}
