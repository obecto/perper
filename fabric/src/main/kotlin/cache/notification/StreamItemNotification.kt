package com.obecto.perper.fabric.cache.notification

class StreamItemNotification(
    var stream: String,
    var parameter: Int,
    var cache: String,
    var key: Long,
    var ephemeral: Boolean,
) : Notification()
