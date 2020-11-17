package com.obecto.perper.fabric.cache.notification
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class CallTriggerNotification(
    var call: String,
) : Notification(), Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("call", call)
    }

    override fun readBinary(reader: BinaryReader) {
        call = reader.readString("call")
    }
}
