package com.obecto.perper.fabric.cache
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class AgentData(
    var delegate: String
) : Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        throw RuntimeException("Did not expect call to writeBinary")
    }

    override fun readBinary(reader: BinaryReader) {
        delegate = reader.readString("delegate")
    }
}
