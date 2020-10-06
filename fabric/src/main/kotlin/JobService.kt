package com.obecto.perper.fabric
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.GlobalScope
import kotlinx.coroutines.Job
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import org.apache.ignite.services.Service
import org.apache.ignite.services.ServiceContext

abstract class JobService : Service {
    lateinit var job: Job

    open override fun init(ctx: ServiceContext) {}

    override fun cancel(ctx: ServiceContext) {
        job.cancel()
        runBlocking {
            job.join()
        }
    }

    override fun execute(ctx: ServiceContext) {
        job = GlobalScope.launch { execute(ctx) }
    }

    abstract suspend fun CoroutineScope.execute(ctx: ServiceContext)
}
