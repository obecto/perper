package com.obecto.perper.fabric.cache

class StreamListener(
    var agentDelegate: String,
    var stream: String,
    var parameter: String,
    var filter: Map<String, Any?>,
    var localToData: Boolean,
)
