package com.obecto.perper.fabric
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.cancel
import kotlinx.coroutines.launch
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceContext
import kotlin.coroutines.EmptyCoroutineContext

abstract class JobService : Service {
    lateinit var coroutineScope: CoroutineScope

    open override fun init(ctx: ServiceContext) {
        coroutineScope = CoroutineScope(EmptyCoroutineContext)
    }

    override fun cancel(ctx: ServiceContext) {
        coroutineScope.cancel()
    }

    override fun execute(ctx: ServiceContext) {
        coroutineScope.launch { execute(ctx) }
    }

    abstract suspend fun CoroutineScope.execute(ctx: ServiceContext)
}
