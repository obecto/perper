package com.obecto.perper.model

fun PerperExecution(execution: String): PerperExecution {
    return PerperExecution.newBuilder().also {
        it.execution = execution
    }.build()
}

fun PerperInstance(agent: String, instance: String): PerperInstance {
    return PerperInstance.newBuilder().also {
        it.agent = agent
        it.instance = instance
    }.build()
}

fun PerperStream(name: String): PerperStream {
    return PerperStream.newBuilder().also {
        it.stream = name
    }.build()
}

fun PerperDictionary(dictionary: String): PerperDictionary {
    return PerperDictionary.newBuilder().also {
        it.dictionary = dictionary
    }.build()
}

fun PerperList(list: String): PerperList {
    return PerperList.newBuilder().also {
        it.list = list
    }.build()
}

fun PerperError(message: String): PerperError {
    return PerperError.newBuilder().also {
        it.message = message
    }.build()
}

fun String?.toPerperErrorOrNull(): PerperError? =
    if (isNullOrEmpty()) { null } else { PerperError(this) }
