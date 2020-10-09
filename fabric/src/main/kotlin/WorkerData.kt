package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryObject
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class WorkerData(
    var name: String,
    var delegate: String,
    var caller: String,
    var params: BinaryObject,
) : Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("caller", caller)
        writer.writeString("delegate", delegate)
        writer.writeString("name", name)
        writer.writeObject("params", params)
    }

    override fun readBinary(reader: BinaryReader) {
        caller = reader.readString("caller")
        delegate = reader.readString("delegate")
        name = reader.readString("name")
        params = reader.readObject("params")
    }
}
