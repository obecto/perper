package com.obecto.perper.fabric
import io.grpc.ForwardingServerCall
import io.grpc.ForwardingServerCallListener
import io.grpc.Metadata
import io.grpc.ServerBuilder
import io.grpc.ServerCall
import io.grpc.ServerCallHandler
import io.grpc.ServerInterceptor
import io.grpc.Status
import io.grpc.protobuf.services.ProtoReflectionService
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.awaitCancellation
import kotlinx.coroutines.channels.Channel
import kotlinx.coroutines.runBlocking
import org.apache.ignite.Ignite
import org.apache.ignite.IgniteLogger
import org.apache.ignite.cache.query.ContinuousQuery
import org.apache.ignite.resources.IgniteInstanceResource
import org.apache.ignite.resources.LoggerResource
import org.apache.ignite.services.ServiceContext
import java.util.concurrent.CancellationException
import java.util.concurrent.TimeUnit
import javax.cache.event.CacheEntryEvent
import javax.cache.event.CacheEntryUpdatedListener

class GrpcService(var port: Int = 40400) : JobService() {
    lateinit var log: IgniteLogger

    lateinit var ignite: Ignite

    @IgniteInstanceResource
    fun setIgniteResource(igniteResource: Ignite?) {
        if (igniteResource != null) {
            ignite = igniteResource
        }
    }

    @LoggerResource
    fun setLoggerResource(loggerResource: IgniteLogger?) {
        if (loggerResource != null) {
            log = loggerResource
        }
    }

    override suspend fun CoroutineScope.execute(ctx: ServiceContext) {
        val server = ServerBuilder.forPort(port).also({
            it.intercept(ExceptionInterceptor())
            it.addService(Grpc1FabricImpl(ignite))
            val grpc2PerperDescriptors = Grpc2FabricProtobufDescriptorsImpl(ignite)
            it.addService(grpc2PerperDescriptors)
            it.addService(Grpc2FabricExecutionsImpl(PerperExecutionsIgniteImpl(ignite), grpc2PerperDescriptors))
            it.addService(Grpc2FabricStreamsImpl(PerperStreamsIgniteImpl(ignite), grpc2PerperDescriptors))
            it.addService(Grpc2FabricStatesListImpl(PerperListsIgniteImpl(ignite), grpc2PerperDescriptors))
            it.addService(Grpc2FabricStatesDictionaryImpl(PerperDictionariesIgniteImpl(ignite), grpc2PerperDescriptors))
            it.addService(GrpcExternalScalerImpl(ignite))
            it.addService(ProtoReflectionService.newInstance())
        }).build()

        server.start()
        log.debug({ "Fabric server started!" })

        try {
            awaitCancellation()
        } finally {
            server.shutdown()
            server.awaitTermination(1L, TimeUnit.SECONDS)
            server.shutdownNow()
            server.awaitTermination()
        }
    }

    private inner class ExceptionInterceptor : ServerInterceptor { // via https://github.com/grpc/grpc-kotlin/issues/141#issuecomment-726829195
        var idx = 0

        override fun <ReqT : Any, RespT : Any> interceptCall(call: ServerCall<ReqT, RespT>, headers: Metadata, next: ServerCallHandler<ReqT, RespT>): ServerCall.Listener<ReqT> {
            idx = idx + 1
            log.trace({ "!$idx!${call.methodDescriptor.getFullMethodName()}! $$" })
            var firstInput: ReqT? = null

            val inner = next.startCall(
                object : ForwardingServerCall.SimpleForwardingServerCall<ReqT, RespT>(call) {
                    override fun sendMessage(message: RespT) {
                        log.trace({ "!$idx!${call.methodDescriptor.getFullMethodName()} --> $message" })
                        super.sendMessage(message)
                    }
                    override fun close(status: Status, trailers: Metadata) {
                        if (status.isOk) {
                            return super.close(status, trailers)
                        }

                        val cause = status.cause

                        if (cause is CancellationException) {
                            val newStatus = Status.CANCELLED.withDescription(cause.message).withCause(cause)
                            return super.close(newStatus, trailers)
                        } else if (cause is Throwable) {
                            log.error("Error in handling GRPC call; call=${call.methodDescriptor.getFullMethodName()} firstInput=$firstInput cause=$cause")
                            cause.printStackTrace()
                            return super.close(status, trailers)
                        }
                    }
                },
                headers
            )
            return object : ForwardingServerCallListener.SimpleForwardingServerCallListener<ReqT>(inner) {
                override fun onMessage(message: ReqT) {
                    if (firstInput == null) firstInput = message
                    log.trace({ "!$idx!${call.methodDescriptor.getFullMethodName()} <-- $message" })
                    super.onMessage(message)
                }
            }
        }
    }

    private fun <T, K, V> ContinuousQuery<K, V>.setChannelLocalListener(channel: Channel<T>, block: suspend Channel<T>.(CacheEntryEvent<out K, out V>) -> Unit): Channel<T> {
        this.localListener = CacheEntryUpdatedListener { events ->
            try {
                for (event in events) {
                    runBlocking { channel.block(event) }
                }
            } catch (e: Exception) {
                channel.close(e)
            }
        }
        return channel
    }
}
