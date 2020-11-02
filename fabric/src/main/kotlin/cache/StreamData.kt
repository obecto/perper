package com.obecto.perper.fabric.cache
import org.apache.ignite.binary.BinaryCollectionFactory
import org.apache.ignite.binary.BinaryMapFactory
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable
import java.sql.Timestamp

class StreamData(
    var agentDelegate: String,
    var delegate: String,
    var delegateType: StreamDelegateType,
    var listeners: List<StreamListener>,
    var indexType: String?,
    var indexFields: LinkedHashMap<String, String>?,
    var ephemeral: Boolean,
) : Binarylizable {
    var lastModified: Timestamp = Timestamp(System.currentTimeMillis())

    override fun writeBinary(writer: BinaryWriter) {
        throw RuntimeException("Did not expect call to writeBinary")
    }

    override fun readBinary(reader: BinaryReader) {
        agentDelegate = reader.readString("agentDelegate")
        delegate = reader.readString("delegate")
        delegateType = reader.readEnum("delegateType")
        ephemeral = reader.readBoolean("ephemeral")
        indexFields = reader.readMap<String, String>("indexFields", BinaryMapFactory { LinkedHashMap(it) }) as LinkedHashMap<String, String>?
        indexType = reader.readString("indexType")
        lastModified = reader.readTimestamp("lastModified")
        @Suppress("UNCHECKED_CAST")
        listeners = reader.readCollection("listeners", BinaryCollectionFactory { ArrayList<StreamListener>(it) }) as List<StreamListener>
    }
}
