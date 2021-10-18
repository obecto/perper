package com.obecto.perper.fabric.cache

class StreamData(
    var agent: String,
    var instance: String,
    var delegate: String,
    var delegateType: StreamDelegateType,
    var parameters: Array<Any?>,
    var ephemeral: Boolean,
    var indexType: String?,
    var indexFields: LinkedHashMap<String, String>?,
    var listeners: ArrayList<StreamListener>,
)
