package com.obecto.perper.fabric.cache.notification

class StreamItemNotification(
    var stream: String,
    var parameter: String,
    var cache: String,
    var index: Long,
    var ephemeral: Boolean,
) : Notification()
