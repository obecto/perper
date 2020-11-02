package com.obecto.perper.fabric.cache.notification
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class StreamItemNotification(
    var stream: String,
    var parameter: String,
    var cache: String,
    var index: Long,
    var ephemeral: Boolean,
) : Notification(), Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("cache", cache)
        writer.writeBoolean("ephemeral", ephemeral)
        writer.writeLong("index", index)
        writer.writeString("parameter", parameter)
        writer.writeString("stream", stream)
    }

    override fun readBinary(reader: BinaryReader) {
        cache = reader.readString("cache")
        ephemeral = reader.readBoolean("ephemeral")
        index = reader.readLong("index")
        parameter = reader.readString("parameter")
        stream = reader.readString("stream")
    }
}
