package com.obecto.perper.fabric.cache

class CallData(
    var agent: String,
    var instance: String,
    var delegate: String,
    // var parameters: Array<Any?>,
    var callerAgent: String,
    var caller: String,
    var localToData: Boolean,
    var finished: Boolean,
    // var result: Array<Any?>?,
    var error: String?,
)
