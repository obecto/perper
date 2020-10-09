package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class StreamParam(
    var stream: String,
    var filter: BinaryObject?,
) : Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeObject("filter", filter)
        writer.writeString("stream", stream)
    }

    override fun readBinary(reader: BinaryReader) {
        filter = reader.readObject("filter")
        stream = reader.readString("stream")
    }
}
