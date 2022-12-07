import uuid
import attr
import time
import asyncio
import grpc
import warnings
from typing import Any
from datetime import datetime, timedelta, timezone
from collections import defaultdict, namedtuple
from google.protobuf.internal.containers import RepeatedCompositeFieldContainer

from .task_collection import TaskCollection
from .proto import fabric_pb2_grpc
from .proto import grpc2_executions_pb2
from grpc2_executions_pb2_grpc import FabricExecutionsStub
from grpc2_model_pb2 import PerperInstance, PerperDictionary, PerperExecution, PerperStream, PerperError
from .proto import grpc2_states_pb2
from grpc2_states_pb2_grpc import FabricStatesDictionaryStub, FabricStatesListStub
from .proto import grpc2_streams_pb2
from grpc2_streams_pb2_grpc import FabricStreamsStub


FabricExecution = namedtuple(
    "Execution", ["agent", "instance", "delegate", "execution", "arguments"])


class factorydict(dict):
    def __init__(self, default_factory):
        self.default_factory = default_factory

    def __missing__(self, key):
        value = self.default_factory(key)
        self[key] = value
        return value


class FabricService:
    CANCELLED = object()
    LISTENER_PERSIST_ALL = -1 << 63
    LISTENER_JUST_TRIGGER = ~(-1 << 63)

    def __init__(self, ignite, grpc_channel):
        self.task_collection = TaskCollection()

        self.grpc_channel = grpc_channel
        self.fabric_stub = fabric_pb2_grpc.FabricStub(self.grpc_channel)
        self.fabric_executions_stub = FabricExecutionsStub(self.grpc_channel)
        self.fabric_dictionary_stub = FabricStatesDictionaryStub(self.grpc_channel)
        self.fabric_streams_stub = FabricStreamsStub(self.grpc_channel)

        self.execution_channels = defaultdict(asyncio.Queue)
        self.execution_tasks = {}

    @classmethod
    def get_current_ticks(cls):
        dt = datetime.now(timezone.utc)
        t = (dt - datetime(1970, 1, 1, tzinfo=timezone.utc)
             ) // timedelta(microseconds=1) * 10
        return t

    @classmethod
    def generate_name(cls, basename=None):
        return f"{basename}-{uuid.uuid4()}"

    async def __aenter__(self):
        return self

    async def __aexit__(self, *args):
        await self.stop()

    async def stop(self):
        self.task_collection.cancel()
        # print(self.task_collection.tasks)
        await self.task_collection.wait()
        await self.grpc_channel.close()

    # INSTANCES:

    async def create_instance(self, agent: str) -> PerperInstance:
        # TODO: Add PerperList API and implement parent-child agent list with it in State, with respective append on Create, 
        # and automatic destroying of children on Remove of parent
        init_instance = PerperInstance(instance=agent, agent="Registry")
        execution = await self.create_execution(init_instance, "Run", [])
        return PerperInstance(instance=execution.execution, agent=agent)

    async def remove_instance(self, instance: PerperInstance):
        return await self.remove_execution(PerperExecution(instance.instance))

    # EXECUTIONS:

    async def create_execution(self, instance: PerperInstance, delegate, parameters) -> PerperExecution:
        execution = PerperExecution(execution=self.generate_name(delegate))

        execution_create_request = grpc2_executions_pb2.ExecutionsCreateRequest(
            execution=execution,
            instance=instance,
            delegate=delegate,
            arguments=parameters)
        await self.fabric_executions_stub.Create(execution_create_request, wait_for_ready=True)
        return execution

    async def remove_execution(self, execution: PerperExecution):
        execution_delete_request = grpc2_executions_pb2.ExecutionsDeleteRequest(
            execution=execution
        ) 
        await self.fabric_executions_stub.Delete(execution_delete_request, wait_for_ready=True)

    async def write_execution_finished(self, execution: PerperExecution):
        execution_complete_request = grpc2_executions_pb2.ExecutionsCompleteRequest(
            execution=execution
        )
        await self.fabric_executions_stub.Complete(execution_complete_request, wait_for_ready=True)

    async def write_execution_result(self, execution: PerperExecution, result):
        execution_complete_request = grpc2_executions_pb2.ExecutionsCompleteRequest(
            execution=execution,
            results=result
        )
        await self.fabric_executions_stub.Complete(execution_complete_request, wait_for_ready=True)

    async def write_execution_error(self, execution: PerperExecution, error: PerperError):
        execution_complete_request = grpc2_executions_pb2.ExecutionsCompleteRequest(
            execution=execution,
            error=error
        )
        await self.fabric_executions_stub.Complete(execution_complete_request, wait_for_ready=True)

    async def read_execution_error_and_result(self, execution: PerperExecution) -> tuple[PerperError, RepeatedCompositeFieldContainer[Any]]:
        execution_result_request = grpc2_executions_pb2.ExecutionsGetResultRequest(
            execution=execution
        )
        execution_result_response: grpc2_executions_pb2.ExecutionsGetResultResponse = await self.fabric_executions_stub.GetResult(
            execution_result_request, wait_for_ready=True)
        return (execution_result_response.error, execution_result_response.results)

    async def listen_executions(self, instance: PerperInstance, delegate, reserve=True, work_group=""):
        async def helper():
            execution_listen_request = grpc2_executions_pb2.ExecutionsListenRequest(
                instance_filter=instance,
                delegate=delegate
            )

            if reserve:
                execution_stream = self.fabric_executions_stub.ListenAndReserve(
                    wait_for_ready=True)

                await execution_stream.write(grpc2_executions_pb2.ExecutionsListenAndReserveRequest(
                    reserve_next=1,
                    filter=execution_listen_request,
                    workgroup=work_group))

                async for proto in execution_stream:
                    yield proto
                    if not proto.deleted and proto.execution != "":
                        await execution_stream.write(grpc2_executions_pb2.ExecutionsListenAndReserveRequest(
                            reserve_next=1
                        ))
            else:
                async for proto in self.fabric_executions_stub.Listen(execution_listen_request, wait_for_ready=True):
                    yield proto

        async for proto in helper():
            if proto.deleted:
                value = self.execution_tasks.setdefault(
                    proto.execution, FabricService.CANCELLED)
                if value is not FabricService.CANCELLED:
                    value.cancel()
                    self.execution_tasks[proto.execution] = FabricService.CANCELLED
            elif proto.execution != "":
                execution = FabricExecution(
                    instance.agent, proto.instance, proto.delegate, proto.execution, proto.arguments)
                yield execution

    # STATE:

    async def create_state(self, dictionary: PerperDictionary, options=None):
        request = grpc2_states_pb2.StatesDictionaryCreateRequest(
            dictionary=dictionary,
            cache_options=options
        )
        await self.fabric_dictionary_stub.Create(request, wait_for_ready=True)
    
    async def clear_state(self, dictionary: PerperDictionary):
        request = grpc2_states_pb2.StatesDictionaryDeleteRequest(
            dictionary=dictionary,
            keep_cache=True
        )
        await self.fabric_dictionary_stub.Delete(request, wait_for_ready=True)
    
    async def destroy_state(self, dictionary: PerperDictionary):
        request = grpc2_states_pb2.StatesDictionaryDeleteRequest(
            dictionary=dictionary
        )
        await self.fabric_dictionary_stub.Delete(request, wait_for_ready=True)

    async def set_state_value(self, dictionary: PerperDictionary, key, value) -> bool:
        request = grpc2_states_pb2.StatesDictionaryOperateRequest(
            dictionary=dictionary,
            key=key,
            set_new_value=True,
            new_value=value
        )
        response: grpc2_states_pb2.StatesDictionaryOperateResponse = await self.fabric_dictionary_stub.Operate(request, wait_for_ready=True)
        return response.operation_successful

    async def remove_state_value(self, dictionary: PerperDictionary, key) -> bool:
        request = grpc2_states_pb2.StatesDictionaryOperateRequest(
            dictionary=dictionary,
            key=key,
            set_new_value=True,
            new_value=None
        )
        response: grpc2_states_pb2.StatesDictionaryOperateResponse = await self.fabric_dictionary_stub.Operate(request, wait_for_ready=True)
        return response.operation_successful

    async def get_state_value(self, dictionary: PerperDictionary, key, default=None):
        request = grpc2_states_pb2.StatesDictionaryOperateRequest(
            dictionary=dictionary,
            key=key,
            get_existing_value=True
        )
        response: grpc2_states_pb2.StatesDictionaryOperateResponse = await self.fabric_dictionary_stub.Operate(request, wait_for_ready=True)
        result = response.previous_value
        if result is None:
            return default
        return result

    # STREAMS:

    async def create_stream(self, stream: PerperStream, ephemeral: bool, action: bool, options=None, query_entities=[]):
        request = grpc2_streams_pb2.StreamsCreateRequest(
            stream=stream,
            ephemeral=ephemeral,
            cache_options=options
        )
        await self.fabric_streams_stub.Create(request, wait_for_ready=True)

        if action:
            move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(
                stream=stream,
                listener_name=f"{stream}-trigger",
                reached_key=self.LISTENER_JUST_TRIGGER
            )
            await self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

    async def remove_stream(self, stream: PerperStream):
        request = grpc2_streams_pb2.StreamsDeleteRequest(stream=stream)
        await self.fabric_streams_stub.Delete(request, wait_for_ready=True)

    async def set_stream_listener_position(self, listener: str, stream: PerperStream, position: int):
        if not listener:
            listener = self.generate_name(stream.stream)

        move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(
            stream=stream,
            listener_name=listener,
            reached_key=position
        )
        await self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

    async def remove_stream_listener(self, stream: PerperStream, listener: str):
        if not listener:
            listener = self.generate_name(stream.stream)

        move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(
            stream=stream,
            listener_name=listener,
            delete_listener=True
        )
        await self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

    async def write_stream_item(self, stream: PerperStream, key, item):
        if key:
            request = grpc2_streams_pb2.StreamsWriteItemRequest(
                stream=stream,
                key=key,
                value=item
            )
        else:
             request = grpc2_streams_pb2.StreamsWriteItemRequest(
                stream=stream,
                auto_key=True,
                value=item
            )
        await self.fabric_streams_stub.WriteItem(request, wait_for_ready=True)

    async def enumerate_stream_items(self, stream: PerperStream, listener: str, start_key=-1, stride=0, local_to_data=False):
        if not listener:
            listener = self.generate_name(stream.stream)

        move_listener_request = grpc2_streams_pb2.StreamsMoveListenerRequest(
            stream=stream,
            listener_name=listener,
            reached_key=self.LISTENER_PERSIST_ALL
        )
        self.fabric_streams_stub.MoveListener(move_listener_request, wait_for_ready=True)

        listen_request = grpc2_streams_pb2.StreamsListenItemsRequest(
            stream=stream,
            listener_name=listener,
            start_from_latest=stream.start_key == -1,
            local_to_data=local_to_data
        )
        listen_response = self.fabric_streams_stub.ListenItems(listen_request, wait_for_ready=True)
        await listen_response.initial_metadata()

        async def helper():
            async for item in listen_response:
                yield (item.key, item.value)
                await self.set_stream_listener_position(listener=listener, stream=stream, position=item.key)

        return helper()

    def query_stream_sql(self, sql, sql_parameters):
        raise NotImplementedError("Query SQL is not implemented")

    # GRPC:

    # async def wait_execution_finished(self, execution):
    #     await self.fabric_stub.ExecutionFinished(fabric_pb2.ExecutionFinishedRequest(execution=execution), wait_for_ready=True)

    # async def wait_listener_attached(self, stream):
    #     await self.fabric_stub.ListenerAttached(fabric_pb2.ListenerAttachedRequest(stream=stream), wait_for_ready=True)

    # async def enumerate_stream_item_keys(self, stream, start_key=-1, stride=0, local_to_data=False):
    #     stream_items = self.fabric_stub.StreamItems(
    #         fabric_pb2.StreamItemsRequest(stream=stream, startKey=start_key, stride=stride, localToData=local_to_data), wait_for_ready=True
    #     )
    #     await stream_items.initial_metadata()

    #     async def helper():
    #         async for item in stream_items:
    #             yield item.key

    #     return helper()

    async def set_execution_task(self, execution, task):
        value = self.execution_tasks.setdefault(execution, task)
        if value is FabricService.CANCELLED:
            task.cancel()
        elif value is not task:
            warnings.warn(
                f"Multiple tasks set for execution '{execution}'", UserWarning)

    # UTILS:

    async def write_stream_item_realtime(self, stream, item):
        await self.write_stream_item(stream, self.get_current_ticks(), item)

    async def write_execution_exception(self, execution, exception):
        return await self.write_execution_error(execution, PerperError(str(exception)))

    async def read_execution_result(self, execution) -> RepeatedCompositeFieldContainer[Any]:
        (error, result) = await self.read_execution_error_and_result(execution)

        if error is not None:
            raise Exception(f"Execution failed with error: {error}")

        return result
