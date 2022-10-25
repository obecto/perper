package com.obecto.perper.fabric.cache

class ExecutionData(
    var agent: String,
    var instance: String,
    var delegate: String,
    var finished: Boolean,
    var parameters: Array<Any>? = null,
    var results: Array<Any>? = null,
    var error: String? = null
)
