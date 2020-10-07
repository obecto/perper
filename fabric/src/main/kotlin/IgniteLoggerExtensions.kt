package com.obecto.perper.fabric
import org.apache.ignite.IgniteLogger

inline fun IgniteLogger.info(f: () -> String) {
    if (isInfoEnabled()) info(f())
}

inline fun IgniteLogger.debug(f: () -> String) {
    if (isInfoEnabled()) info(f()) // FIXME: Change logging to something which supports logging levels..
//     if (isDebugEnabled()) debug(f())
}

inline fun IgniteLogger.trace(f: () -> String) {
    if (isTraceEnabled()) trace(f())
}
