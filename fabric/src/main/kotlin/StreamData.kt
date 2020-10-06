package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryObject
import java.util.Date

class StreamData(
    val name: String,
    val delegate: String,
    val delegateType: StreamDelegateType,
    val params: BinaryObject,
    val streamParams: Map<String, List<String>>,
    val indexType: String?,
    val indexFields: List<Pair<String, String>>?,
    val workers: Map<String, WorkerData>,
) {
    var lastModified: Date = Date()
}
