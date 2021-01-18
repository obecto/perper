package com.obecto.perper.fabric.cache

class StreamListener(
    var agentDelegate: String,
    var stream: String,
    var parameter: Int,
    var filter: Map<String, Any?>,
    var replay: Boolean,
    var localToData: Boolean,
)
