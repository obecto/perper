package com.obecto.perper.fabric.cache.notification

import org.apache.ignite.cache.affinity.AffinityKeyMapped

abstract class NotificationKey

data class NotificationKeyLong(
    var key: Long,
    @JvmField
    @AffinityKeyMapped
    var affinity: Long
) : NotificationKey()

data class NotificationKeyString(
    var key: Long,
    @JvmField
    @AffinityKeyMapped
    var affinity: String
) : NotificationKey()

fun NotificationKey(key: Long, affinity: Any): NotificationKey = when (affinity) {
    is Long -> NotificationKeyLong(key, affinity)
    is String -> NotificationKeyString(key, affinity)
    else -> throw Error("Unexpected affinity type ${affinity.javaClass}")
}
