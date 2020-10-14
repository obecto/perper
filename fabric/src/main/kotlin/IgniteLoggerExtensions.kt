package com.obecto.perper.fabric
import org.apache.ignite.IgniteLogger

inline fun IgniteLogger.info(f: () -> String) {
    if (isInfoEnabled()) info(f())
}

inline fun IgniteLogger.debug(f: () -> String) {
    if (isDebugEnabled()) debug(f())
}

inline fun IgniteLogger.trace(f: () -> String) {
    if (isTraceEnabled()) trace(f())
}
