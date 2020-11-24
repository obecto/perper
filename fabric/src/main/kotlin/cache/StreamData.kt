package com.obecto.perper.fabric.cache

class StreamData(
    var agentDelegate: String,
    var delegate: String,
    var delegateType: StreamDelegateType,
    var listeners: List<StreamListener>,
    var indexType: String?,
    var indexFields: LinkedHashMap<String, String>?,
    var ephemeral: Boolean,
)
