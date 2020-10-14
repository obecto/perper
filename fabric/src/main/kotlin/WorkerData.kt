package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class WorkerData(
    var name: String,
    var delegate: String,
    var caller: String,
    var finished: Boolean,
) : Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        throw RuntimeException("Did not expect call to writeBinary")
    }

    override fun readBinary(reader: BinaryReader) {
        caller = reader.readString("caller")
        delegate = reader.readString("delegate")
        finished = reader.readBoolean("finished")
        name = reader.readString("name")
    }
}
