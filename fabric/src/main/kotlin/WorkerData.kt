package com.obecto.perper.fabric
import org.apache.ignite.binary.BinaryObject

class WorkerData(
    val name: String,
    val delegate: String,
    val caller: String,
    val params: BinaryObject,
)
