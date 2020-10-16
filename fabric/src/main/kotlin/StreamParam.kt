package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class StreamParam(
    var stream: String,
    var filter: Map<String, Any?>,
    var localToData: Boolean
) : Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        throw RuntimeException("Did not expect call to writeBinary")
    }

    override fun readBinary(reader: BinaryReader) {
        filter = reader.readMap<String, Any?>("filter")
        stream = reader.readString("stream")
        localToData = reader.readBoolean("localToData")
    }
}
