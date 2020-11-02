package com.obecto.perper.fabric.cache.notification
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class CallResultNotification(
    var call: String,
    var caller: String,
) : Notification(), Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("call", call)
        writer.writeString("caller", caller)
    }

    override fun readBinary(reader: BinaryReader) {
        call = reader.readString("call")
        caller = reader.readString("caller")
    }
}
