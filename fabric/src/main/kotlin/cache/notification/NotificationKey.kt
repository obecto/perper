package com.obecto.perper.fabric.cache.notification

import org.apache.ignite.cache.affinity.AffinityKeyMapped

abstract class NotificationKey

data class NotificationKeyLong(
    // NOTE: Names and order of the fields are important!
    @JvmField
    @AffinityKeyMapped
    var affinity: Long,
    var key: Long,
) : NotificationKey()

data class NotificationKeyString(
    // NOTE: Names and order of the fields are important!
    @JvmField
    @AffinityKeyMapped
    var affinity: String,
    var key: Long,
) : NotificationKey()

fun NotificationKey(key: Long, affinity: Any): NotificationKey = when (affinity) {
    is Long -> NotificationKeyLong(affinity, key)
    is String -> NotificationKeyString(affinity, key)
    else -> throw Error("Unexpected affinity type ${affinity.javaClass}")
}
