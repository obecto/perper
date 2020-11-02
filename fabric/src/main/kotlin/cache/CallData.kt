package com.obecto.perper.fabric.cache
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

class CallData(
    var agent: String,
    var agentDelegate: String,
    var delegate: String,
    var callerAgentDelegate: String,
    var caller: String,
    var finished: Boolean,
    var localToData: Boolean
) : Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        throw RuntimeException("Did not expect call to writeBinary")
    }

    override fun readBinary(reader: BinaryReader) {
        agent = reader.readString("agent")
        agentDelegate = reader.readString("agentDelegate")
        caller = reader.readString("caller")
        callerAgentDelegate = reader.readString("callerNamespace")
        delegate = reader.readString("delegate")
        finished = reader.readBoolean("finished")
        localToData = reader.readBoolean("localToData")
    }
}
