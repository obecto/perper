package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryReader
import org.apache.ignite.binary.BinaryWriter
import org.apache.ignite.binary.Binarylizable

open class StreamNotification()

class StreamTriggerNotification() : StreamNotification(), Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
    }

    override fun readBinary(reader: BinaryReader) {
    }
}

class StreamItemNotification(
    var parameter: String,
    var cache: String,
    var index: Long,
) : StreamNotification(), Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("cache", cache)
        writer.writeLong("index", index)
        writer.writeString("parameter", parameter)
    }

    override fun readBinary(reader: BinaryReader) {
        cache = reader.readString("cache")
        index = reader.readLong("index")
        parameter = reader.readString("parameter")
    }
}

class WorkerResultNotification(
    var worker: String,
) : StreamNotification(), Binarylizable {
    override fun writeBinary(writer: BinaryWriter) {
        writer.writeString("worker", worker)
    }

    override fun readBinary(reader: BinaryReader) {
        worker = reader.readString("worker")
    }
}
