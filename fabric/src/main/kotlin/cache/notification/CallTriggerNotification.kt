package com.obecto.perper.fabric.cache.notification

class CallTriggerNotification(
    var call: String,
    val instance: String,
    var delegate: String
) : Notification()
