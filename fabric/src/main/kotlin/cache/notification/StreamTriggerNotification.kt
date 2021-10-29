package com.obecto.perper.fabric.cache.notification

class StreamTriggerNotification(
    var stream: String,
    val instance: String,
    var delegate: String
) : Notification()
