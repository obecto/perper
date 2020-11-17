package com.obecto.perper.fabric.cache.notification
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class StreamTriggerNotification(
    var stream: String,
) : Notification(), Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("stream", stream)
    }

    override fun readBinary(reader: BinaryReader) {
        stream = reader.readString("stream")
    }
}
