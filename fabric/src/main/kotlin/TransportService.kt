package com.obecto.perper.fabric
import org.apache.ignite.services.ServiceContext

class TransportService : JobService() {
    override suspend fun executeJob(ctx: ServiceContext) {
    }

    fun sendStreamTrigger(receiverStream: String, receiverDelegate: String) {
        println(arrayOf(receiverStream, receiverDelegate))
    }

    fun sendWorkerResult(receiverStream: String, receiverDelegate: String, workerName: String) {
        println(arrayOf(receiverStream, receiverDelegate, workerName))
    }

    fun sendWorkerTrigger(receiverStream: String, receiverDelegate: String, workerName: String) {
        println(arrayOf(receiverStream, receiverDelegate, workerName))
    }

    fun sendItemUpdate(receiverStream: String, receiverDelegate: String, receiverParameter: String, senderStream: String, senderItem: Long) {
        println(arrayOf(receiverStream, receiverDelegate, receiverParameter, senderStream, senderItem))
    }
}
