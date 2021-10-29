package com.obecto.perper.fabric.cache

class StreamListener(
    var callerAgent: String,
    var callerInstance: String,
    var caller: String,
    var parameter: Int,
    var filter: HashMap<String, Any?>?,
    var replay: Boolean,
    var localToData: Boolean,
)
